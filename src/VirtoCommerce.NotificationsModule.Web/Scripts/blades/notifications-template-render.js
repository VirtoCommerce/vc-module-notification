angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.templateRenderController',
        ['$rootScope', '$scope', '$sce', '$localStorage', 'platformWebApp.bladeNavigationService',
            'platformWebApp.dialogService', 'virtoCommerce.notificationsModule.notificationsModuleApi',
            'platformWebApp.authService', 'platformWebApp.accounts', 'virtoCommerce.notificationsModule.sendTestEmailService',
            function ($rootScope, $scope, $sce, $localStorage, bladeNavigationService,
                dialogService, notifications, authService, accounts, sendTestEmailService) {

        var blade = $scope.blade;
        var keyTemplateLocalStorage;

        blade.dynamicProperties = '';
        blade.originHtml = '';
        $scope.error = null;
        $scope.viewResponse = { full: false };

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
                $scope.error = null;
                $("#notification_template_preview").on("load", function() {
                    $('#notification_template_preview').height($('#notification_template_preview').contents().outerHeight());
                });
                blade.originHtml = $sce.trustAsHtml("<html><body>" + response.html + "</body></html>");
            }, function (error) {
                $scope.error = error;
            });           
            blade.isLoading = false;
        };

        $scope.errorAsString = function () {
            return JSON.stringify($scope.error, null, 2);
        };

        

        blade.sendTestEmail = function () {
            sendTestEmailService.showDialogAndSendTestEmail(
                blade.notification.type,
                blade.language,
                blade.currentEntity.body,
                blade.data
            );
        }

        $scope.blade.toolbarCommands = [{
            name: "notifications.commands.share-preview",
            icon: 'fa fa-envelope',
            executeMethod: function () {
                blade.sendTestEmail();
            },
            canExecuteMethod: function () {
                return true;
            },
            permission: 'notifications:templates:read'
        }];


        blade.headIcon = 'fa fa-eye';

        blade.initialize();
    }]);
