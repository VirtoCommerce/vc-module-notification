angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationJournalDetailsContentController',
        ['$scope', '$sce', '$timeout',
            function ($scope, $sce, $timeout) {
                var blade = $scope.blade;
                blade.title = blade.currentEntity.subject;
                // Defer the preview payload to a later tick. Assigned synchronously, the iframe's
                // srcdoc is written in the same task in which the iframe is attached, so the initial
                // about:srcdoc navigation (started from the raw, not-yet-interpolated attribute) wins
                // and the frame keeps showing the literal placeholder. The other preview blades work
                // only because they assign inside an async render callback.
                $timeout(function () {
                    blade.html = $sce.trustAsHtml(blade.currentEntity.body);
                    blade.isLoading = false;
                });
            }
        ]
    );
