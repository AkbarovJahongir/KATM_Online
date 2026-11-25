using Newtonsoft.Json;

namespace CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 24. Сведения об остатках и оборотах по балансовым счетам (CI-024, ТЗ 10.0)
    /// </summary>
    public class CreditRegistrationBalanceReport
    {
        /// <summary>
        /// Головной код организации (014)
        /// </summary>
        [JsonProperty("pHead")]
        public string? PHead { get; set; }

        /// <summary>
        /// Дата баланса (yyyy-MM-dd'T'HH:mm:ss'Z')
        /// </summary>
        [JsonProperty("pDate")]
        public string? PDate { get; set; }

        /// <summary>
        /// Филиалы
        /// </summary>
        [JsonProperty("pDepartmentArray")]
        public List<DepartmentBalance>? PDepartmentArray { get; set; }
    }

    public class DepartmentBalance
    {
        /// <summary>
        /// Код филиала (012)
        /// </summary>
        [JsonProperty("code")]
        public string? Code { get; set; }

        /// <summary>
        /// Остатки по балансовым счетам филиала
        /// </summary>
        [JsonProperty("coaBalanceArray")]
        public List<CoaBalance>? CoaBalanceArray { get; set; }
    }

    public class CoaBalance
    {
        /// <summary>
        /// Балансовый счёт (019)
        /// </summary>
        [JsonProperty("coa")]
        public string? Coa { get; set; }

        /// <summary>
        /// Код валюты (017)
        /// </summary>
        [JsonProperty("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// Дата (yyyy-MM-dd'T'HH:mm:ss'Z')
        /// </summary>
        [JsonProperty("date")]
        public string? Date { get; set; }

        [JsonProperty("startBalance")]
        public decimal? StartBalance { get; set; }

        [JsonProperty("debit")]
        public decimal? Debit { get; set; }

        [JsonProperty("credit")]
        public decimal? Credit { get; set; }

        [JsonProperty("endBalance")]
        public decimal? EndBalance { get; set; }
    }
}
