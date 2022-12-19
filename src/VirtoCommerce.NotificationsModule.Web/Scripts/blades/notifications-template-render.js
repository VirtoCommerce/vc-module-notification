angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.templateRenderController', ['$rootScope', '$scope', '$sce', '$localStorage', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'virtoCommerce.notificationsModule.notificationsModuleApi', 'platformWebApp.authService', 'platformWebApp.accounts',
        function ($rootScope, $scope, $sce, $localStorage, bladeNavigationService, dialogService, notifications, authService, accounts) {

        var blade = $scope.blade;
        var keyTemplateLocalStorage;

        blade.dynamicProperties = '';
        blade.originHtml = '';

        $scope.setForm = function (form) {
            $scope.formScope = form;
        }

        function pluckAddress(address) {
            return address ? _(address).pluck('value') : address;
        }

        blade.initialize = function () {

            blade.isLoading = true;

            blade.language = blade.languageCode ? blade.languageCode : 'default';
            
            blade.data = angular.copy(blade.notification);
            if (blade.currentEntity.sample && blade.currentEntity.sample!="") {
                var sample = JSON.parse(blade.currentEntity.sample);
                angular.extend(blade.data, sample);
            }

            blade.data.cc = pluckAddress(blade.data.cc);
            blade.data.bcc = pluckAddress(blade.data.bcc);

            keyTemplateLocalStorage = blade.tenantType + '.' + blade.notification.type + '.' + blade.language;
            var itemFromLocalStorage = $localStorage[keyTemplateLocalStorage];
            if (itemFromLocalStorage) {
                blade.notification.context = itemFromLocalStorage;
            }

            notifications.renderTemplate({
                type: blade.notification.type,
                language: blade.language
            }, {
                text: blade.currentEntity.body,
                data: blade.data,
                notificationLayoutId: blade.currentEntity.notificationLayoutId
            }, function (response) {
                $('#notification_template_preview').load(function() {
                    $('#notification_template_preview').height($('#notification_template_preview').contents().outerHeight());
                });
                blade.originHtml = $sce.trustAsHtml("<html><body>" + response.html + "</body></html>");
            });           
            blade.isLoading = false;
        };

        function sharePreview(eMailTo) {
            delete blade.data.cc;
            delete blade.data.bcc;
            blade.data.languageCode = blade.language;
            blade.data.to = eMailTo;

            notifications.sharePreview({
                type: blade.notification.type,
                language: blade.language
            }, {
                text: blade.currentEntity.body,
                data: blade.data
            }, function (response) {
                if (response.isSuccess) {
                    var dialog = {
                        id: "shareSuccess",
                        title: 'notifications.dialogs.share-success.title',
                        message: 'notifications.dialogs.share-success.message'
                    };
                    dialogService.showSuccessDialog(dialog);
                }
                else {
                    dialog = {
                        id: "shareError",
                        title: 'notifications.dialogs.share-error.title',
                        message: response.errorMessage
                    };
                    dialogService.showErrorDialog(dialog);
                }                
            });
        }


        blade.sharePreview = function () {
            var eMailTo = authService.userLogin;
            accounts.get({ id: authService.userLogin }, function (data) {
                eMailTo = data.email;
                var dialog = {
                    id: "confirmSharePreview",
                    email: eMailTo,
                    setEmail: function (email) {
                        sharePreview(email);
                    }
                }
                dialogService.showDialog(dialog, 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notification-templates-share-preview-dialog.tpl.html', 'platformWebApp.confirmDialogController');
            });

        }

        $scope.blade.toolbarCommands = [{
            name: "notifications.commands.share-preview",
            icon: 'fa fa-share-alt-square',
            executeMethod: function () {
                blade.sharePreview();
            },
            canExecuteMethod: function () {
                return true;
            },
            permission: 'notifications:templates:read'
        }];


        blade.headIcon = 'fa fa-eye';

        blade.initialize();
    }]);
