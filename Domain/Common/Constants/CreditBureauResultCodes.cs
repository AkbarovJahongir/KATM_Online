namespace Domain.Common.Constants
{
    public class CreditBureauResultCodes
    {
        public const string SUCCESS_00000 = "00000";
        public const string SUCCESS_05000 = "05000";
        public const string WAIT_AND_TRY_AGAIN = "05050";
        public const string NO_TOKEN_FOUND = "05002";
        public const string IDENTICAL_REQUEST = "05003";
        public const string WAITING_OPERATOR_CONFIRMATION = "05004";
        /// <summary>
        /// Субъект не дает согласия на получение кредитной истории, подключена услуга Freeze. Субъекту необходимо отключить услугу Freeze.
        /// </summary>
        public const string FREEZE_SERVICE_ACTIVE = "05555";
    }
}
