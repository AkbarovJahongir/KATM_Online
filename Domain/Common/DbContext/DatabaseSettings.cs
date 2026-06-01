namespace Domain.Common.DbContext
{
    public class DatabaseSettings
    {
        // Db abs
        public string DBConnection { get; set; } = null!;
        public string DBPassword { get; set; } = null!;
        // Db CIB
        public string CIBConnection { get; set; } = null!;
        public string CIBPassword { get; set; } = null!;
        public string Key { get; set; } = "a24ab1yw4d5e2024pasd1ea2000B9999";
    }
}
