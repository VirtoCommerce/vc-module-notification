namespace VirtoCommerce.NotificationsModule.Core.Model.Result
{
    public abstract class OperationResult
    {
    }

    public class ErrorResult : OperationResult
    {
        public string Message { get; private set; }

        public ErrorResult(string message) => Message = message;
    }

    public class SuccessResult : OperationResult
    {
    }
}
