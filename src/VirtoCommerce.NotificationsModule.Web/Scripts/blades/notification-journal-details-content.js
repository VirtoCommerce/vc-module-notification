angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationJournalDetailsContentController',
        ['$scope', '$sce',
            function ($scope, $sce) {
                var blade = $scope.blade;
                blade.html = $sce.trustAsHtml(blade.currentEntity.body);
                blade.isLoading = false;
            }
        ]
    );
