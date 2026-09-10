angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationJournalDetailsContentController',
        ['$scope', '$sce', '$timeout',
            function ($scope, $sce, $timeout) {
                var blade = $scope.blade;
                blade.title = blade.currentEntity.subject;       
                $timeout(function () {
                    blade.html = $sce.trustAsHtml(blade.currentEntity.body);
                    blade.isLoading = false;
                });
            }
        ]
    );
