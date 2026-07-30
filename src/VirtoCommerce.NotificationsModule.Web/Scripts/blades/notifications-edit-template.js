var htmlBeautify = require('js-beautify').html;

// The platform's CodeMirror ships without the fold addons, so foldGutter/Ctrl-Q below would be
// inert. These patch the platform's global instance (see webpack NormalModuleReplacementPlugin).
require('codemirror/addon/fold/foldcode');
require('codemirror/addon/fold/foldgutter');
require('codemirror/addon/fold/brace-fold');     // JSON / javascript mode
require('codemirror/addon/fold/xml-fold');       // htmlmixed mode
require('codemirror/addon/fold/comment-fold');
require('codemirror/addon/fold/foldgutter.css');

// Register a Liquid-over-HTML CodeMirror mode once: HTML (htmlmixed) highlighting with a
// {{ … }} / {% … %} overlay so Liquid tags are visually distinct.
(function () {
    var CM = window.CodeMirror;
    if (CM && CM.defineMode && CM.overlayMode && !(CM.modes && CM.modes['liquid-html'])) {
        CM.defineMode('liquid-html', function (config) {
            var liquidOverlay = {
                token: function (stream) {
                    if (stream.match(/\{\{[\s\S]*?\}\}/)) { return 'liquid-output'; }
                    if (stream.match(/\{%[\s\S]*?%\}/)) { return 'liquid-tag'; }
                    while (stream.next() != null) {
                        if (stream.match('{{', false) || stream.match('{%', false)) { break; }
                    }
                    return null;
                }
            };
            return CM.overlayMode(CM.getMode(config, 'htmlmixed'), liquidOverlay);
        });
    }
})();

// Shared across editor blades so only the blade that wrote the deep-link query string last is
// allowed to clear it (blades can overlap while one is closing and another is opening).
var deepLinkWriteSeq = 0;
var deepLinkOwnerToken = null;

// Shared "does any notification layout exist?" probe, so opening several template editors does not
// repeat the same request (see loadLayouts).
var LAYOUTS_EXIST_TTL_MS = 30000;
var layoutsExistPromise = null;
var layoutsExistCheckedAt = 0;

angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.editTemplateController',
        ['$rootScope', '$scope', '$timeout', '$sce', '$location', '$translate', '$localStorage', 'virtoCommerce.notificationsModule.notificationsModuleApi',
            'virtoCommerce.notificationsModule.notificationLayoutsApi', 'FileUploader', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService',
            'platformWebApp.authService', 'platformWebApp.accounts', 'virtoCommerce.notificationsModule.sendTestEmailService',
        function ($rootScope, $scope, $timeout, $sce, $location, $translate, $localStorage, notifications,
            layouts, FileUploader, bladeNavigationService, dialogService,
            authService, accounts, sendTestEmailService) {
            var blade = $scope.blade;
            $scope.isValid = false;

            var formScope;
            $scope.setForm = function (form) {
                formScope = form;
            }

            var codemirrorEditor;
            blade.dynamicProperties = '';

            // ================= Editor workspace: tabs, live preview, JSON editor =================
            var PREFS_KEY = 'notificationsModule.editorPrefs';
            var prefs = $localStorage[PREFS_KEY] || {};
            blade.activeTab = prefs.activeTab || 'template';   // 'template' | 'sample' | 'preview'

            function savePrefs() {
                $localStorage[PREFS_KEY] = { activeTab: blade.activeTab };
            }

            // CodeMirror-based editors mis-measure themselves if laid out while hidden,
            // so force a redraw when a pane becomes visible.
            function refreshEditors() {
                $timeout(function () {
                    try { window.dispatchEvent(new Event('resize')); } catch (e) { }
                    if (templateEditor) { templateEditor.refresh(); }
                    if (sampleEditor) { sampleEditor.refresh(); }
                });
            }

            // ui-codemirror applies the initial ng-model value with setValue(), which CodeMirror
            // records as an undoable step. Without dropping it, the first Ctrl+Z on an untouched
            // template wipes the whole body. Called after the initial load and whenever the blade
            // swaps the edited template/language.
            function clearEditorHistory() {
                // Deferred so the new content has been pushed into the editors before we drop history.
                $timeout(function () {
                    if (templateEditor) { templateEditor.clearHistory(); }
                    if (sampleEditor) { sampleEditor.clearHistory(); }
                });
            }

            // Full-screen (Option B): left pane = template OR sample, right pane = live preview.
            blade.fullscreen = false;
            blade.fsLeft = 'template';   // 'template' | 'sample'

            $scope.paneVisible = function (pane) {
                if (blade.fullscreen) {
                    return pane === blade.fsLeft || pane === 'preview';
                }
                return blade.activeTab === pane;
            };

            function isPreviewVisible() {
                return blade.fullscreen || blade.activeTab === 'preview';
            }

            $scope.setActiveTab = function (tab) {
                blade.activeTab = tab;
                savePrefs();
                refreshEditors();
                if (isPreviewVisible()) { schedulePreview(); }
            };

            $scope.setFsLeft = function (which) {
                blade.fsLeft = which;
                refreshEditors();
            };

            // Keyboard support for the tablist (WAI-ARIA tabs pattern): Enter/Space activate,
            // Left/Right move between tabs, Home/End jump to the first/last one.
            function focusTab(tab) {
                $timeout(function () {
                    var el = document.getElementById(`nt-tab-${tab}`);
                    if (el) { el.focus(); }
                });
            }

            function handleTabKeydown(event, tabs, current, activate) {
                var code = event.which || event.keyCode;
                if (code === 13 || code === 32) {            // Enter / Space
                    event.preventDefault();
                    activate(current);
                    return;
                }
                if (code !== 37 && code !== 39 && code !== 36 && code !== 35) { return; }
                event.preventDefault();
                var next;
                if (code === 36) {                          // Home
                    next = 0;
                } else if (code === 35) {                    // End
                    next = tabs.length - 1;
                } else {                                     // Left / Right
                    var delta = code === 39 ? 1 : -1;
                    next = (tabs.indexOf(current) + delta + tabs.length) % tabs.length;
                }
                activate(tabs[next]);
                focusTab(tabs[next]);
            }

            $scope.onTabKeydown = function (event, tab) {
                handleTabKeydown(event, ['template', 'sample', 'preview'], tab, $scope.setActiveTab);
            };

            $scope.onFsTabKeydown = function (event, tab) {
                handleTabKeydown(event, ['template', 'sample'], tab, $scope.setFsLeft);
            };

            function applyFullscreen(on) {
                blade.fullscreen = on;
                // Keep the editor the user was working in on the left when entering full-screen.
                if (on && (blade.activeTab === 'template' || blade.activeTab === 'sample')) {
                    blade.fsLeft = blade.activeTab;
                }
                // Raise the main content stacking context above the platform nav (see CSS).
                angular.element(document.body).toggleClass('nt-fullscreen-active', on);
                if (on) {
                    window.addEventListener('keydown', onFsKeydown, true);
                    schedulePreview();
                } else {
                    window.removeEventListener('keydown', onFsKeydown, true);
                }
                refreshEditors();
            }

            function isAutocompleteOpen() {
                return !!(templateEditor && templateEditor.state && templateEditor.state.completionActive)
                    || !!document.querySelector('.CodeMirror-hints');
            }

            function onFsKeydown(e) {
                if (e.keyCode === 27 && blade.fullscreen) { // Esc
                    // Runs in the capture phase, i.e. before CodeMirror's own Esc handler removes the
                    // hint widget — checking afterwards always looked "closed" and exited full-screen
                    // on the same key. First Esc closes the popup, the next one leaves full-screen.
                    if (isAutocompleteOpen()) { return; }
                    $scope.$applyAsync(function () { applyFullscreen(false); });
                }
            }

            $scope.toggleFullscreen = function () {
                applyFullscreen(!blade.fullscreen);
            };

            $scope.$on('$destroy', function () {
                window.removeEventListener('keydown', onFsKeydown, true);
                angular.element(document.body).removeClass('nt-fullscreen-active');
                if (previewTimer) { $timeout.cancel(previewTimer); }
                clearDeepLink();
            });

            // ---- Live preview (debounced) ----
            var previewTimer;
            var previewRequestId = 0;
            var previewRequestsInFlight = 0;
            blade.previewHtml = $sce.trustAsHtml('');
            blade.previewError = null;

            // The preview fires while the user is still typing, so a half-written Liquid expression
            // is expected to fail. Keep those failures inline in the Preview pane instead of letting
            // the platform paint the blade-wide error band (which also covers the blade's ✕).
            var unbindHttpError = $rootScope.$on('httpError', function (event, rejection) {
                var url = rejection && rejection.config && rejection.config.url;
                if (previewRequestsInFlight > 0 && url && url.indexOf('rendercontent') !== -1) {
                    event.preventDefault();
                }
            });
            $scope.$on('$destroy', unbindHttpError);

            function schedulePreview() {
                if (previewTimer) { $timeout.cancel(previewTimer); }
                previewTimer = $timeout(updatePreview, 500);
            }

            function updatePreview() {
                // Bump the request id up front so any in-flight render (and any state change that
                // returns early below) invalidates older responses — avoids stale previews.
                var requestId = ++previewRequestId;

                if (!blade.currentEntity || blade.notification.kind !== 'EmailNotification') { return; }
                if (!blade.isSampleValidJson()) { blade.previewError = { invalidJson: true }; return; }

                var data = angular.copy(blade.notification);
                if (blade.currentEntity.sample && blade.currentEntity.sample !== '') {
                    angular.extend(data, JSON.parse(blade.currentEntity.sample));
                }

                previewRequestsInFlight++;
                notifications.renderTemplate({
                    type: blade.notification.type,
                    language: blade.currentEntity.languageCode ? blade.currentEntity.languageCode : 'default'
                }, {
                    text: blade.currentEntity.body,
                    data: data,
                    notificationLayoutId: blade.currentEntity.notificationLayoutId
                }, function (response) {
                    previewRequestsInFlight--;
                    if (requestId !== previewRequestId) { return; }
                    blade.previewError = null;
                    blade.previewHtml = $sce.trustAsHtml(`<html><body>${response.html}</body></html>`);
                }, function (error) {
                    previewRequestsInFlight--;
                    if (requestId !== previewRequestId) { return; }
                    blade.previewError = error;
                });
            }

            $scope.$watchGroup(
                ['blade.currentEntity.body', 'blade.currentEntity.sample', 'blade.currentEntity.notificationLayoutId', 'blade.currentEntity.languageCode'],
                function () { if (isPreviewVisible()) { schedulePreview(); } });

            var LABELS_PREFIX = 'notifications.blades.notifications-edit-template.labels.';

            // Translation helper for this blade's labels.
            function t(key) {
                return $translate.instant(LABELS_PREFIX + key);
            }

            // The notify dialog translates title/message itself ({{ title | translate: titleValues }}),
            // so hand it keys plus the parser text as an interpolation value.
            function showFormatError(id, messageKey, error) {
                dialogService.showNotificationDialog({
                    id: id,
                    title: `${LABELS_PREFIX}${messageKey}-title`,
                    message: LABELS_PREFIX + messageKey,
                    messageValues: { details: error.message }
                });
            }

            // CodeMirror gutter ids shared by the JSON and HTML editors.
            var CM_GUTTER_LINES = 'CodeMirror-linenumbers';
            var CM_GUTTER_FOLD = 'CodeMirror-foldgutter';

            // ---- Sample data JSON editor (reuses the platform JSON editor pattern) ----
            var sampleEditor = null;
            $scope.jsonEditorOptions = {
                lineWrapping: true,
                lineNumbers: true,
                mode: { name: 'javascript', json: true },
                extraKeys: {
                    'Ctrl-Q': function (cm) { cm.foldCode(cm.getCursor()); },
                    'Ctrl-Alt-F': function () { formatSampleJson(); $scope.$applyAsync(); }
                },
                foldGutter: true,
                gutters: [CM_GUTTER_LINES, CM_GUTTER_FOLD],
                onLoad: function (_editor) {
                    sampleEditor = _editor;
                    _editor.setOption('readOnly', !!(blade.currentEntity && blade.currentEntity.isReadonly));
                    injectFormatButton(_editor);
                    $timeout(function () {
                        _editor.refresh();
                        _editor.clearHistory();
                    });
                }
            };
            if (typeof window.jsonlint !== 'undefined') {
                $scope.jsonEditorOptions.lint = {
                    // Sample data is empty on every user-created template, and jsonlint reports
                    // empty input as "got 'EOF'". Treat empty as neutral so a freshly opened
                    // template doesn't greet the user with a parse error it can't act on.
                    getAnnotations: function (text, options, cm) {
                        if (!text || !text.trim()) { return []; }
                        var jsonLint = window.CodeMirror.helpers && window.CodeMirror.helpers.lint && window.CodeMirror.helpers.lint.json;
                        return jsonLint ? jsonLint(text, options, cm) : [];
                    }
                };
                $scope.jsonEditorOptions.gutters = [CM_GUTTER_LINES, CM_GUTTER_FOLD, 'CodeMirror-lint-markers'];
            }

            function injectFormatButton(_editor) {
                var wrapper = angular.element(_editor.getWrapperElement());
                wrapper.css('position', 'relative');

                var container = angular.element('<div class="json-controls"></div>');
                container.css({ position: 'absolute', top: '5px', right: '5px', zIndex: '10', display: 'flex', gap: '5px' });

                var commonStyle = { padding: '2px 8px', fontSize: '12px', borderRadius: '3px', height: '24px', lineHeight: '20px', boxSizing: 'border-box', border: 'none' };

                var statusIndicator = angular.element('<div class="json-btn status-indicator"></div>');
                statusIndicator.css(angular.extend({}, commonStyle, { backgroundColor: '#F44336', color: 'white', display: 'none', alignItems: 'center', justifyContent: 'center' }));

                var jsonLabel = t('format-json');
                var formatBtn = angular.element('<button type="button" class="json-btn format-btn"></button>');
                formatBtn.attr('title', `${jsonLabel} (Ctrl+Alt+F)`).text(jsonLabel);
                formatBtn.css(angular.extend({}, commonStyle, { backgroundColor: '#43b0e6', color: 'white', cursor: 'pointer' }));
                formatBtn.on('click', function () { formatSampleJson(); $scope.$apply(); });

                container.append(statusIndicator);
                container.append(formatBtn);
                wrapper.prepend(container);

                var changeHandler = function () { updateIndicator(statusIndicator, _editor); };
                _editor.on('change', changeHandler);
                $timeout(function () { updateIndicator(statusIndicator, _editor); }, 100);

                $scope.$on('$destroy', function () { if (_editor) { _editor.off('change', changeHandler); } });
            }

            function updateIndicator(indicator, editor) {
                var content = editor.getValue();
                if (!content) { indicator.css('display', 'none'); return; }
                try {
                    JSON.parse(content);
                    indicator.css('display', 'none');
                } catch (e) {
                    indicator.css('display', 'flex');
                    indicator.text(t('invalid-json'));
                }
            }

            function formatSampleJson() {
                if (!blade.currentEntity || blade.currentEntity.isReadonly) { return; }
                try {
                    blade.currentEntity.sample = JSON.stringify(JSON.parse(blade.currentEntity.sample || '{}'), null, 2);
                } catch (e) {
                    showFormatError('jsonFormatError', 'format-json-error', e);
                }
            }
            $scope.formatSampleJson = formatSampleJson;

            // ---- Template editor: HTML/Liquid CodeMirror (replaces the markdown htmleditor) ----
            var templateEditor = null;
            $scope.htmlEditorOptions = {
                lineNumbers: true,
                lineWrapping: true,
                mode: 'liquid-html',
                foldGutter: true,
                gutters: [CM_GUTTER_LINES, CM_GUTTER_FOLD],
                matchTags: { bothTags: true },
                autoCloseTags: true,
                extraKeys: {
                    'Ctrl-Q': function (cm) { cm.foldCode(cm.getCursor()); },
                    'Ctrl-Alt-F': function () { formatHtml(); $scope.$applyAsync(); },
                    'Ctrl-Space': function (cm) { showHints(cm); }
                },
                onLoad: function (_editor) {
                    templateEditor = _editor;
                    _editor.setOption('readOnly', !!(blade.currentEntity && blade.currentEntity.isReadonly));
                    injectHtmlFormatButton(_editor);
                    wireHints(_editor);
                    $timeout(function () {
                        _editor.refresh();
                        _editor.clearHistory();
                    });
                }
            };

            function formatHtml() {
                if (!blade.currentEntity || blade.currentEntity.isReadonly) { return; }
                try {
                    blade.currentEntity.body = htmlBeautify(blade.currentEntity.body || '', {
                        indent_size: 2,
                        preserve_newlines: true,
                        max_preserve_newlines: 2,
                        wrap_line_length: 0,
                        content_unformatted: ['pre', 'style', 'script']
                    });
                } catch (e) {
                    showFormatError('htmlFormatError', 'format-html-error', e);
                }
            }
            $scope.formatHtml = formatHtml;

            function injectHtmlFormatButton(_editor) {
                var wrapper = angular.element(_editor.getWrapperElement());
                wrapper.css('position', 'relative');
                var container = angular.element('<div class="json-controls"></div>');
                container.css({ position: 'absolute', top: '5px', right: '5px', zIndex: '10', display: 'flex', gap: '5px' });
                var htmlLabel = t('format-html');
                var btn = angular.element('<button type="button" class="json-btn format-btn"></button>');
                btn.attr('title', `${htmlLabel} (Ctrl+Alt+F)`).text(htmlLabel);
                btn.css({ padding: '2px 8px', fontSize: '12px', borderRadius: '3px', height: '24px', lineHeight: '20px', boxSizing: 'border-box', border: 'none', backgroundColor: '#43b0e6', color: 'white', cursor: 'pointer' });
                btn.on('click', function () { formatHtml(); $scope.$apply(); });
                container.append(btn);
                wrapper.prepend(container);
            }

            // ---- Autocomplete: {{ variables }} and {% tags %} in the template editor ----
            // Common Liquid tags; each completion inserts the tag (with its closing tag where relevant).
            var LIQUID_TAGS = [
                { label: 'if', text: '{% if condition %}\n\n{% endif %}' },
                { label: 'if / else', text: '{% if condition %}\n\n{% else %}\n\n{% endif %}' },
                { label: 'elsif', text: '{% elsif condition %}' },
                { label: 'else', text: '{% else %}' },
                { label: 'unless', text: '{% unless condition %}\n\n{% endunless %}' },
                { label: 'for', text: '{% for item in collection %}\n\n{% endfor %}' },
                { label: 'case', text: '{% case variable %}\n{% when value %}\n\n{% endcase %}' },
                { label: 'when', text: '{% when value %}' },
                { label: 'assign', text: '{% assign variable = value %}' },
                { label: 'capture', text: '{% capture variable %}\n\n{% endcapture %}' },
                { label: 'comment', text: '{% comment %}\n\n{% endcomment %}' },
                { label: 'include', text: "{% include 'template' %}" },
                { label: 'raw', text: '{% raw %}\n\n{% endraw %}' },
                { label: 'break', text: '{% break %}' },
                { label: 'continue', text: '{% continue %}' },
                { label: 'endif', text: '{% endif %}' },
                { label: 'endfor', text: '{% endfor %}' },
                { label: 'endunless', text: '{% endunless %}' },
                { label: 'endcase', text: '{% endcase %}' },
                { label: 'endcapture', text: '{% endcapture %}' },
                { label: 'endcomment', text: '{% endcomment %}' },
                { label: 'endraw', text: '{% endraw %}' }
            ];

            function variableHint(cm) {
                var CM = window.CodeMirror;
                var cur = cm.getCursor();
                var line = cm.getLine(cur.line);
                var before = line.slice(0, cur.ch);
                var open = before.lastIndexOf('{{');
                if (open === -1 || before.slice(open + 2).indexOf('}}') !== -1) { return null; }
                var typed = before.slice(open + 2).replace(/^\s+/, '').toLowerCase();
                var toCh = cur.ch;
                var closing = line.slice(cur.ch).match(/^\s*\}\}/);
                if (closing) { toCh += closing[0].length; }
                var list = ($scope.sampleVariables || [])
                    .filter(function (v) { return !typed || v.toLowerCase().indexOf(typed) !== -1; })
                    .map(function (v) { return { text: `{{ ${v} }}`, displayText: v }; });
                if (!list.length) { return null; }
                return { list: list, from: CM.Pos(cur.line, open), to: CM.Pos(cur.line, toCh) };
            }

            function tagHint(cm) {
                var CM = window.CodeMirror;
                var cur = cm.getCursor();
                var line = cm.getLine(cur.line);
                var before = line.slice(0, cur.ch);
                var open = before.lastIndexOf('{%');
                if (open === -1 || before.slice(open + 2).indexOf('%}') !== -1) { return null; }
                var typed = before.slice(open + 2).replace(/^\s+/, '').toLowerCase();
                var toCh = cur.ch;
                var closing = line.slice(cur.ch).match(/^\s*%\}/);
                if (closing) { toCh += closing[0].length; }
                var list = LIQUID_TAGS
                    .filter(function (t) { return !typed || t.label.toLowerCase().indexOf(typed) !== -1; })
                    .map(function (t) { return { text: t.text, displayText: t.label }; });
                if (!list.length) { return null; }
                return { list: list, from: CM.Pos(cur.line, open), to: CM.Pos(cur.line, toCh) };
            }

            // Pick variable vs tag completion based on the nearest opener before the cursor.
            function showHints(cm) {
                var cur = cm.getCursor();
                var before = cm.getLine(cur.line).slice(0, cur.ch);
                var openTag = before.lastIndexOf('{%');
                var openVar = before.lastIndexOf('{{');
                if (openTag > -1 && openTag > openVar && before.slice(openTag + 2).indexOf('%}') === -1) {
                    cm.showHint({ hint: tagHint, completeSingle: false });
                } else {
                    cm.showHint({ hint: variableHint, completeSingle: false });
                }
            }

            function wireHints(_editor) {
                _editor.on('inputRead', function (inst) {
                    var before = inst.getLine(inst.getCursor().line).slice(0, inst.getCursor().ch);
                    if (/\{%\s*\w*$/.test(before)) {
                        inst.showHint({ hint: tagHint, completeSingle: false });
                    } else if (/\{\{\s*[\w.]*$/.test(before)) {
                        inst.showHint({ hint: variableHint, completeSingle: false });
                    }
                });
            }

            // ---- Variable extraction for template autocomplete ----
            // Flatten the sample JSON into dot paths, converting each key to the Liquid name the
            // Scriban renderer actually exposes (StandardMemberRenamer): CustomerOrder -> customer_order.
            $scope.sampleVariables = [];

            function toLiquidName(name) {
                var out = '';
                for (var i = 0; i < name.length; i++) {
                    var c = name[i];
                    if (c >= 'A' && c <= 'Z') {
                        var prevUpper = i > 0 && name[i - 1] >= 'A' && name[i - 1] <= 'Z';
                        var nextLower = i + 1 < name.length && name[i + 1] >= 'a' && name[i + 1] <= 'z';
                        // Match Scriban's StandardMemberRenamer: break before a capital that starts a
                        // new word, including the last capital of an acronym (HTMLContent -> html_content).
                        if (i > 0 && (!prevUpper || nextLower)) { out += '_'; }
                        out += c.toLowerCase();
                    } else {
                        out += c;
                    }
                }
                return out;
            }

            function buildVariables() {
                var vars = [];
                if (blade.currentEntity && blade.currentEntity.sample) {
                    try {
                        (function walk(obj, prefix) {
                            angular.forEach(obj, function (value, key) {
                                var seg = toLiquidName(key);
                                var path = prefix ? `${prefix}.${seg}` : seg;
                                if (value && typeof value === 'object' && !angular.isArray(value)) {
                                    walk(value, path);
                                } else {
                                    vars.push(path);
                                }
                            });
                        })(JSON.parse(blade.currentEntity.sample), '');
                    } catch (e) { /* invalid JSON -> no variables */ }
                }
                $scope.sampleVariables = vars;
            }
            $scope.$watch('blade.currentEntity.sample', buildVariables);

            function saveTemplate() {
                var date = new Date();
                var now = date.getFullYear() + '-' + ('0' + (date.getMonth() + 1)).slice(-2) + '-' + ('0' + date.getDate()).slice(-2);

                if (blade.isNew) {
                    blade.currentEntity.createdDateAsString = now;
                    blade.currentEntity.isReadonly = false;
                    blade.currentEntity.id = blade.currentEntity.languageCode ? null : blade.currentEntity.id;
                    blade.origEntity = angular.copy(blade.currentEntity);
                }
                else {
                    blade.currentEntity.modifiedDateAsString = now;
                    blade.origEntity = angular.copy(blade.currentEntity);
                }

                var sameLanguageTemplate = _.filter(blade.notification.templates, function (template) { return template.languageCode == blade.currentEntity.languageCode; })
                var hasPredefinedTemplates = _.any(sameLanguageTemplate, function (template) { return template.isPredefined });

                if (hasPredefinedTemplates) {
                    blade.currentEntity.isPredefined = true;
                    blade.currentEntity.isEdited = true;
                } else {
                    blade.currentEntity.isPredefined = false;
                }

                var ind = blade.notification.templates.findIndex(function (element) {
                    return (element.languageCode == blade.currentEntity.languageCode)
                        && (element.isPredefined == blade.currentEntity.isPredefined && element.isEdited == blade.currentEntity.isEdited);
                });

                if (ind >= 0) {
                    blade.notification.templates[ind] = blade.currentEntity;
                }
                else {
                    blade.notification.templates.push(blade.currentEntity);
                }
            }

            function restoreTemplate(template) {
                var dialog = {
                    id: "confirmResetTemplates",
                    template: template,
                    callback: function (confirmed) {
                        if (confirmed) {
                            deleteTemplate(template);
                        }
                    }
                }
                dialogService.showDialog(dialog, 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notification-templates-list-reset-dialog.tpl.html', 'platformWebApp.confirmDialogController');
            }

            function deleteTemplate(template) {
                var index = blade.notification.templates.findIndex(function (element) {
                    return (element.languageCode == template.languageCode && element.isPredefined == template.isPredefined && element.isEdited == template.isEdited);
                });

                if (index > -1) {
                    blade.notification.templates.splice(index, 1);
                }

                // Restore removes the edited override, which must reach the server too.
                persistNotification();
            }

            // When opened from a deep link there is no notification-details / template-list parent,
            // so there is nothing to refresh.
            function refreshParentBlade() {
                if (blade.parentBlade && angular.isFunction(blade.parentBlade.initialize)) {
                    blade.parentBlade.initialize();
                }
            }

            // The notification-details blade keeps cc/bcc as [{ value: 'a@b.c' }] for its editors,
            // while the API expects a plain string array. A deep-linked editor loads the notification
            // straight from the API, so it already holds strings — normalise both shapes.
            function pluckAddressValues(addresses) {
                if (!addresses) { return addresses; }
                return _.map(addresses, function (address) {
                    return address && angular.isObject(address) ? address.value : address;
                });
            }

            // The template editor owns persistence: toolbar Save writes the notification straight
            // away, whether the blade was reached by drill-down or by deep link.
            function persistNotification() {
                blade.isLoading = true;

                var entityToSave = angular.copy(blade.notification);
                entityToSave.cc = pluckAddressValues(entityToSave.cc);
                entityToSave.bcc = pluckAddressValues(entityToSave.bcc);
                entityToSave.templates = _.filter(entityToSave.templates, { isReadonly: false });
                entityToSave.templates.forEach(function (element) {
                    // Need to set IsPredefined to false in order to save the template to the database
                    if (!!element.isEdited && !!element.isPredefined) {
                        element.isPredefined = false;
                    }
                });

                notifications.updateNotification({ type: blade.notification.type }, entityToSave, function () {
                    blade.isLoading = false;
                    rebaselineParentNotification();
                    refreshParentBlade();
                    $scope.bladeClose();
                }, function (error) {
                    blade.isLoading = false;
                    bladeNavigationService.setError('Error: ' + (error.data ? error.data.message : error.status), blade);
                });
            }

            // We just persisted the notification the details blade holds in memory, so move its
            // baseline forward — otherwise it still reports unsaved changes and its Undo would
            // silently revert an edit that is already on the server.
            function rebaselineParentNotification() {
                for (var parent = blade.parentBlade; parent; parent = parent.parentBlade) {
                    if (parent.currentEntity === blade.notification && angular.isFunction(parent.updateNotification)) {
                        parent.origEntity = angular.copy(parent.currentEntity);
                        return;
                    }
                }
            }

            $scope.saveChanges = function () {
                saveTemplate();
                persistNotification();
            };

            //todo
            var contentType = 'image';//blade.contentType.substr(0, 1).toUpperCase() + blade.contentType.substr(1, blade.contentType.length - 1);
            $scope.fileUploader = new FileUploader({
                url: 'api/assets?folderUrl=cms-content/' + contentType + '/assets',
                headers: { Accept: 'application/json' },
                autoUpload: true,
                removeAfterUpload: true,
                onBeforeUploadItem: function (fileItem) {
                    blade.isLoading = true;
                },
                onSuccessItem: function (fileItem, response, status, headers) {
                    $scope.$broadcast('filesUploaded', { items: response });
                },
                onErrorItem: function (fileItem, response, status, headers) {
                    bladeNavigationService.setError(fileItem._file.name + ' failed: ' + (response.message ? response.message : status), blade);
                },
                onCompleteAll: function () {
                    blade.isLoading = false;
                }
            });

            function setTemplate() {
                if (!blade.currentEntity) {
                    blade.currentEntity = { kind: blade.notification.kind };
                }

                blade.isLoading = false;
                if (blade.currentEntity && blade.currentEntity.languageCode === undefined) {
                    blade.currentEntity.languageCode = null;
                }

                $timeout(function () {
                    if (codemirrorEditor) {
                        codemirrorEditor.refresh();
                        codemirrorEditor.focus();
                    }
                    blade.origEntity = angular.copy(blade.currentEntity);
                    buildVariables();
                    refreshEditors();
                    clearEditorHistory();
                    if (isPreviewVisible()) { schedulePreview(); }
                }, 1);

                $scope.isValid = false;
            };

            blade.initialize = function () {
                blade.isLoading = true;
                var found = blade.editedTemplate || _.find(blade.notification.templates, function (templ) { return templ.languageCode === blade.languageCode });
                if (found) {
                    blade.currentEntity = angular.copy(found);
                    blade.origEntity = angular.copy(blade.currentEntity);
                    blade.orightml = blade.currentEntity.body;
                }

                setTemplate();
                loadLayouts();
                updateDeepLink();
            };

            // Reflect the open template in the query string so the URL can be copied/shared.
            // Cleared again on blade close (see $destroy). The state uses reloadOnSearch:false,
            // so these writes don't rebuild the blade stack.
            var deepLinkToken = null;

            function updateDeepLink() {
                if (blade.notification && blade.notification.type) {
                    deepLinkToken = ++deepLinkWriteSeq;
                    deepLinkOwnerToken = deepLinkToken;
                    $location.search('type', blade.notification.type);
                    $location.search('templateId', (blade.currentEntity && blade.currentEntity.id) || null);
                }
            }

            // Blade teardown runs outside a digest, so $location writes made there would sit
            // unflushed until some unrelated event triggered one — leaving the address bar pointing
            // at a closed blade (so F5 reopens it). Schedule the clear on $rootScope, which outlives
            // this scope. Only the blade that wrote the params last clears them, so closing a blade
            // never wipes the query string a newly opened editor just wrote.
            function clearDeepLink() {
                if (deepLinkToken === null || deepLinkOwnerToken !== deepLinkToken) { return; }
                deepLinkToken = null;
                deepLinkOwnerToken = null;
                $rootScope.$applyAsync(function () {
                    $location.search('type', null);
                    $location.search('templateId', null);
                });
            }

            // Hide the Layout field entirely when no layouts exist — the ui-scroll-drop-down
            // renders a dimmed/empty control otherwise. The answer is the same for every blade, so
            // the probe is shared and cached instead of re-requested on each open (it otherwise
            // duplicated what ui-scroll-drop-down already fetches). The short TTL lets a layout
            // created later in the session show up without a page reload.
            blade.hasLayouts = false;
            function loadLayouts() {
                if (blade.notification.kind !== 'EmailNotification') { return; }

                if (!layoutsExistPromise || (Date.now() - layoutsExistCheckedAt) > LAYOUTS_EXIST_TTL_MS) {
                    layoutsExistCheckedAt = Date.now();
                    layoutsExistPromise = layouts.searchNotificationLayouts({ skip: 0, take: 1 }).$promise
                        .then(function (data) {
                            return !!(data && (data.totalCount > 0 || (data.results && data.results.length)));
                        }, function () {
                            layoutsExistPromise = null;   // let the next open retry after a failure
                            return false;
                        });
                }

                layoutsExistPromise.then(function (exists) { blade.hasLayouts = exists; });
            }

            blade.renderTemplate = function () {
                var newBlade = {
                    id: 'renderTemplate',
                    title: 'notifications.blades.notifications-template-render.title',
                    subtitle: 'notifications.blades.notifications-template-render.subtitle',
                    subtitleValues: { type: blade.notificationType },
                    notification: blade.notification,
                    tenantId: blade.tenantId,
                    tenantType: blade.tenantType,
                    currentEntity: blade.currentEntity,
                    languageCode: blade.currentEntity.languageCode,
                    controller: 'virtoCommerce.notificationsModule.templateRenderController',
                    template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notifications-template-render.tpl.html'
                };

                bladeNavigationService.showBlade(newBlade, blade);
            }

            blade.sendTestEmail = function () {
                var data = angular.copy(blade.notification);

                if (blade.currentEntity.sample && blade.currentEntity.sample != "") {
                    var sample = JSON.parse(blade.currentEntity.sample);
                    angular.extend(data, sample);
                }

                sendTestEmailService.showDialogAndSendTestEmail(
                    blade.notification.type,
                    blade.currentEntity.languageCode ? blade.currentEntity.languageCode : 'default',
                    blade.currentEntity.body,
                    data);
            };

            blade.isSampleValidJson = function () {
                if (blade.currentEntity.sample && blade.currentEntity.sample!="") {
                    try {
                        JSON.parse(blade.currentEntity.sample);
                    } catch (e) {
                        return false;
                    }
                }
                return true;
            }

            $scope.blade.toolbarCommands = [
                {
                    name: "platform.commands.save",
                    icon: 'fa fa-save',
                    executeMethod: function () {
                        $scope.saveChanges();
                    },
                    canExecuteMethod: function () {
                        return $scope.isValid;
                    }
                },
                {
                    name: "platform.commands.preview",
                    icon: "fa fa-eye",
                    executeMethod: function () {
                        blade.renderTemplate();
                    },
                    canExecuteMethod: canPreview,
                    permission: "notifications:templates:read"
                },
                {
                    name: "notifications.commands.share-preview",
                    icon: "fa fa-envelope",
                    executeMethod: function () {
                        blade.sendTestEmail();
                    },
                    canExecuteMethod: canPreview,
                    permission: 'notifications:templates:read'
                },
                {
                    name: "notifications.commands.restore",
                    icon: "fa fa-history",
                    executeMethod: function () {
                        restoreTemplate(blade.currentEntity);
                    },
                    canExecuteMethod: function () {
                        return blade.currentEntity.isPredefined && blade.currentEntity.isEdited;
                    },
                    permission: "notifications:template:delete"
                }
            ];

            function isDirty() {
                return (!angular.equals(blade.origEntity, blade.currentEntity) || blade.isNew) && blade.hasUpdatePermission();
            }

            function canRender() {
                return formScope && formScope.$valid && blade && blade.isSampleValidJson() && blade.origEntity && !blade.origEntity.isReadonly;
            }

            // Preview / send-test don't modify the template, so they stay enabled even for
            // read-only (predefined) templates — only a valid form + valid sample JSON are needed.
            function canPreview() {
                return formScope && formScope.$valid && blade.isSampleValidJson();
            }
            
            $scope.$watch("blade.currentEntity", function () {
                $scope.isValid = isDirty() && canRender();
            }, true);

            $scope.searchNotificationLayouts = function (criteria) {
                return layouts.searchNotificationLayouts(criteria);
            }

            // The bottom OK/Cancel bar is gone, so ✕ is the only Cancel affordance — confirm before
            // discarding edits, the same way the notification details blade does.
            blade.onClose = function (closeCallback) {
                bladeNavigationService.showConfirmationIfNeeded(isDirty(), $scope.isValid, blade, $scope.saveChanges, closeCallback,
                    'notifications.dialogs.notification-details-save.title', 'notifications.dialogs.notification-details-save.message');
            };

            blade.headIcon = 'fa fa-envelope';

            blade.initialize();
        }]);
