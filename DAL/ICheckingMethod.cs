namespace AccountManager.DAL
{
    public interface ICheckingMethod
    {
        /// <summary>
        /// 返回Cookie是否有效
        /// </summary>
        /// <returns></returns>
        bool CookieChecking(string Cookie_Json);
    }
}
