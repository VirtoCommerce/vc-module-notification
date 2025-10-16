angular.module('virtoCommerce.notificationsModule')
    .factory('virtoCommerce.notificationsModule.sendTestEmailService', ['platformWebApp.dialogService', 'virtoCommerce.notificationsModule.notificationsModuleApi', 'platformWebApp.authService', 'platformWebApp.accounts',
        function(dialogService, notifications, authService, accounts) {
            var service = {
                sendTestEmail: sendTestEmail,
                showDialogAndSendTestEmail: showDialogAndSendTestEmail
            };
            return service;

            function sendTestEmail(notificationType, language, templateBody, data, emailTo) {
                // Clone data to avoid modifying the original
                var emailData = angular.copy(data);
                delete emailData.cc;
                delete emailData.bcc;
                emailData.languageCode = language;
                emailData.to = emailTo;

                return notifications.sharePreview({
                    type: notificationType,
                    language: language
                }, {
                    text: templateBody,
                    data: emailData
                }, function(response) {
                    if (response.isSuccess) {
                        var dialog = {
                            id: "shareSuccess",
                            title: 'notifications.dialogs.share-success.title',
                            message: 'notifications.dialogs.share-success.message'
                        };
                        dialogService.showSuccessDialog(dialog);
                    } else {
                        var errorDialog = {
                            id: "shareError",
                            title: 'notifications.dialogs.share-error.title',
                            message: response.errorMessage
                        };
                        dialogService.showErrorDialog(errorDialog);
                    }

                    return response;
                });
            }

            function showDialogAndSendTestEmail(notificationType, language, templateBody, data) {
                var eMailTo = authService.userLogin;

                return accounts.get({
                    id: authService.userLogin
                }, function(userData) {
                    eMailTo = userData.email;
                    var dialog = {
                        id: "confirmSharePreview",
                        email: eMailTo,
                        setEmail: function(email) {
                            sendTestEmail(notificationType, language, templateBody, data, email);
                        }
                    }
                    dialogService.showDialog(dialog, 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notification-templates-share-preview-dialog.tpl.html', 'platformWebApp.confirmDialogController');
                });
            }
        }
    ]);