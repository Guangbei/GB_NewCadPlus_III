namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 自动加载和初始化类
    /// </summary>
    /* 默认自动加载和初始化为注释掉的状态，如果你需要可以取消注释
    public class Init : AutoLoad
    {
        public override void Initialize()
        {

            Env.Print("loading...");
            // 如果需要将程序的目录加入信任路径，将下行代码取消注释
            // AppendSupportPath(CurrentDirectory.FullName);
        }

        public override void Terminate()
        {
            // 这里不能调用输出函数，因为这个函数执行的时候，已经没有editor对象了。
            // 所以如果不是想要在cad关闭的时候清理某些东西，这里不用写任何的代码。

        }
    }
    */
}