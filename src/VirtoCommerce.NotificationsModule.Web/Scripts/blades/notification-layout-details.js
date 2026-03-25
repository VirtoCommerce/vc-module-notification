angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationLayoutController',
        ['$scope', '$timeout', 'virtoCommerce.notificationsModule.notificationLayoutsApi', 'platformWebApp.bladeNavigationService', 'FileUploader',
            function ($scope, $timeout, layouts, bladeNavigationService, FileUploader) {
                var blade = $scope.blade;
                blade.headIcon = 'fa fa-envelope';
                blade.updatePermission = 'notifications:update';

                // since the vc-uk-htmleditor directive doesn't fully track changes in the data source
                // force redraw editor directive on the blade after modifiying currentEntity
                function reloadEditor() {
                    $scope.editorReloaded = true;
                }

                if (blade.isNew) {
                    blade.title = 'notifications.blades.notification-layout-details.title-new';
                    blade.currentEntity = {};
                } else {
                    blade.subtitle = 'notifications.blades.notification-layout-details.subtitle';
                }

                blade.refresh = function (parentRefresh) {
                    reloadEditor();

                    if (blade.isNew) {
                        blade.currentEntity = {};
                        blade.isLoading = false;
                    } else {
                        blade.isLoading = true;

                        layouts.getNotificationLayout({ id: blade.currentEntityId }, initializeBlade);

                        if (parentRefresh) {
                            blade.parentBlade.refresh(true);
                        }
                    }
                };

                function initializeBlade(data) {
                    blade.currentEntity = angular.copy(data);
                    blade.originalEntity = data;

                    if (!blade.isNew) {
                        blade.title = blade.currentEntity.name;
                    }

                    blade.isLoading = false;
                }

                $scope.setForm = function (form) {
                    $scope.formScope = form;
                }

                $scope.saveChanges = function () {
                    blade.isLoading = true;

                    if (blade.isNew) {
                        layouts.createNotificationLayout(blade.currentEntity,
                            function () {
                                blade.parentBlade.refresh(true);
                                $scope.bladeClose();
                            });
                    } else {
                        layouts.updateNotificationLayout(blade.currentEntity,
                            function (result) {
                                if (result && result.id) {
                                    blade.currentEntityId = result.id;
                                    blade.refresh(true);
                                } else {
                                    blade.isLoading = false;
                                    bladeNavigationService.setError('Unexpected response when updating notification layout', blade);
                                }
                            },
                            function (error) {
                                blade.isLoading = false;
                                var errorMessage = (error && error.message) ? error.message : 'Error while updating notification layout';
                                bladeNavigationService.setError(errorMessage, blade);
                            });
                    }
                };

                $scope.fileUploader = new FileUploader({
                    url: 'api/assets?folderUrl=cms-content/layouts/assets',
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
                        bladeNavigationService.setError(`${fileItem._file.name} failed: ${(response.message ? response.message : status)}`, blade);
                    },
                    onCompleteAll: function () {
                        blade.isLoading = false;
                    }
                });

                blade.toolbarCommands = [
                    {
                        name: "platform.commands.save",
                        icon: 'fas fa-save',
                        executeMethod: $scope.saveChanges,
                        canExecuteMethod: canSave,
                        permission: blade.updatePermission
                    }];

                if (!blade.isNew) {
                    blade.toolbarCommands.push(
                        {
                            name: "platform.commands.reset",
                            icon: 'fa fa-undo',
                            executeMethod: function () {
                                angular.copy(blade.originalEntity, blade.currentEntity);
                                $scope.editorReloaded = false;
                                $timeout(reloadEditor, 0);
                            },
                            canExecuteMethod: isDirty,
                            permission: blade.updatePermission
                        },
                        {
                            name: "notifications.commands.reset-to-default",
                            icon: 'fa fa-refresh',
                            executeMethod: function () {
                                layouts.resetNotificationLayout({ id: blade.currentEntity.id }, function () {
                                    blade.currentEntityId = blade.currentEntity.name;
                                    blade.refresh(true);
                                });
                            },
                            canExecuteMethod: isPredefinedOverridden,
                            permission: blade.updatePermission
                        }
                    );
                }

                function canSave() {
                    return isDirty() && $scope.formScope && $scope.formScope.$valid;
                }

                function isDirty() {
                    return !angular.equals(blade.currentEntity, blade.originalEntity) && blade.hasUpdatePermission();
                }

                // A layout is a DB-override of a predefined one when the server marks it as predefined
                // but it has a DB record (id !== name means it was saved, not served from memory)
                function isPredefinedOverridden() {
                    var entity = blade.currentEntity;
                    return entity && entity.isPredefined && entity.id !== entity.name;
                }

                blade.refresh(false);
            }]);
