using Autodesk.AutoCAD.Windows;

namespace GB_NewCadPlus_III
{
    public partial class FormMain : Form
    {

        //private bool isTabPageVisible = true; // 联动TabPage的可见状态  DDimLinear
        /// <summary>
        /// 主程序入口 
        /// </summary>
        public FormMain()
        {
            InitializeComponent();
            //设置面板为透明色；不加这行，容易报异常；
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            //记录运行时间
            textBox_cmdShow.Text = DateTime.Now.ToString();
            Command.sendSum = textShow;
            // 注册到统一管理器
            UnifiedUIManager.SetWinFormInstance(this);
            //初始化图层
            NewTjLayer();
        }

        #region 系统变量
        public static void NewTjLayer()
        {
            while (true)
            {
                foreach (var item in VariableDictionary.GGtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.GtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.AtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.StjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.PtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.NtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.EtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.ZKtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.tjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                break;
            }
        }

        /// <summary>
        /// 单选按键选中键名
        /// </summary>
        public string? checkRadioButtonsText = null;
        /// <summary>
        /// 实现面板
        /// </summary>
        public class GB_CadToolsForm
        {
            #region 窗体实现
            /// <summary>
            /// 创建cad里的一个空的窗体
            /// </summary>
            public static PaletteSet? Cad_PaletteSet = null;      //创建cad里的一个空的窗体
            /// <summary>
            /// 初始化一个容器内的工具实体；
            /// </summary>
            static private FormMain GB_ToolsPanel = new FormMain();   //初始化一个容器内的工具实体；
            /// <summary>
            /// 初始化一个容器GB_ToolPanel
            /// </summary>
            public static GB_CadToolsForm GB_ToolsForm = new GB_CadToolsForm(); //初始化一个容器GB_ToolPanel

            /// <summary>
            /// 一个GB_CadToolPanel容器
            /// </summary>
            /// <returns>返回一个单一窗体</returns>
            static public FormMain GB_CadToolPanel()
            {
                return GB_ToolsPanel;//返回一个工具容器
            }


            /// <summary>
            /// 显示工具panel
            /// </summary>
            static public void ShowToolsPanel()
            {
                if (Cad_PaletteSet == null || Cad_PaletteSet.IsDisposed)
                {
                    Cad_PaletteSet = new PaletteSet("图库管理");//初始化这个图库管理窗体；
                    //Cad_PaletteSet.Size = new Size(350, 700);//初始化窗体的大小
                    Cad_PaletteSet.MinimumSize = new System.Drawing.Size(300, 650);//初始化窗体时最小的尺寸
                    //设置为子窗体；
                    GB_CadToolPanel().Anchor =
                       System.Windows.Forms.AnchorStyles.Left |
                       System.Windows.Forms.AnchorStyles.Right |
                       System.Windows.Forms.AnchorStyles.Top;
                    GB_CadToolPanel().Dock = DockStyle.Fill;       //子面板整体覆盖
                    GB_CadToolPanel().TopLevel = false;//子窗体是不是为顶级窗体；
                    GB_CadToolPanel().Location = new System.Drawing.Point(0, 0);//相对位置，左上角，是有设计图纸的区域里；
                    Cad_PaletteSet.Add("屏幕菜单", GB_CadToolPanel());
                }
                Cad_PaletteSet.Visible = true;//显示面板；
                Cad_PaletteSet.Dock = DockSides.Left;//绑定在左侧；
            }
            /// <summary>
            /// 打开的文件列表
            /// </summary>
            private GB_CadToolsForm()
            {
                GetPath.ListDwgFile = new List<string>();
                Load();
            }

            #endregion

            /// <summary>
            /// 拿到本程序的自己路径
            /// </summary>
            /// <returns></returns>
            static private string Path()
            {
                string path = GetPath.GetSelftUserPath();//实例化本程序的自己的路径
                System.IO.Directory.CreateDirectory(path);//在本程序下创建文件夹
                return System.IO.Path.Combine(GetPath.GetSelftUserPath(), "TuKu.txt");//返回本程序的自己路径； 
            }
            /// <summary>
            /// 读取本地设置路径下的配置文件
            /// </summary>
            public void Load()
            {
                string[]? lines = null;
                try
                {
                    lines = System.IO.File.ReadAllLines(Path());//按每一行为一个DWG文件读进来； 
                    GetPath.ListDwgFile.AddRange(lines);//把本程序下添加的文件都显示在列表里；
                }
                catch
                {
                }
            }
            /// <summary>
            /// 保存添加的图库文件与写入配置文件中
            /// </summary>
            public void Save()
            {
                try
                {
                    using (var sr = new StreamWriter(Path())) //useing调用后主动释放文件
                    {
                        foreach (var item in GetPath.ListDwgFile)
                        {
                            sr.WriteLine(item);
                        }
                    }
                }
                catch (System.Exception)
                {
                }
            }
        }

        #region 无用方法
        /// <summary>
        /// 刷新按键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void referenceBtn_Click(object sender, EventArgs e)
        {
            //Env.Editor.WriteMessage("ReferenceCopy ");
            Env.Document.SendStringToExecute("CopyAndSync1 ", false, false, false);
        }

        /// <summary>
        /// 添加入列表
        /// </summary>
        /// <param name="item"></param>
        /// <param name="bUpdate"></param>
        public void AddFilesToList(string item, bool bUpdate = true)
        {
            this.listView_blockList.BeginUpdate();
            string strName = System.IO.Path.GetFileNameWithoutExtension(item);
            ListViewItem itemView = new ListViewItem(strName);
            itemView.SubItems.Add(item);
            this.listView_blockList.Items.Add(itemView);
            this.listView_blockList.EndUpdate();
        }

        /// <summary>
        /// 列队内文件选择事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void listView_Block_SelectedIndexChanged(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("GB_CadToolsInsertBlk ", false, false, false);//给cad发送一个命令"TuKu_TuKuSub "并执行;
            //上面的命令 = Application.DocumentManager.MdiActiveDocument.SendStringToExecute()解释：基类.文档管理.当前打开的文件.发送命令（）
        }

        /// <summary>
        /// 将计算的结果在textBox_show中显示
        /// </summary>
        /// <param name="text"></param>
        public void textShow(string text)
        {
            textBox_show.Text = text;
        }

        /// <summary>
        /// 工具
        /// </summary>
        public void Tools()
        {
            try
            {
                var doc = Acap.DocumentManager.MdiActiveDocument;//拿到当前打开的文档； 
                var db = doc.Database;//拿到当前文档的数据库文件； 
                var db2 = HostApplicationServices.WorkingDatabase;//也是可以拿到当前打开文档的数据库；
                var ed = doc.Editor;//拿到当前文档的命令行； 

                //IFoxCAD的简单命令行
                var iFoxdoc = Env.Document;//拿到当前打开的文档； 
                var iFoxDb = Env.Database;//拿到当前打开文档的数据库；
                var iFoxEd = Env.Editor;//拿到当前文档的命令行；
                Env.OrthoMode = true;//正交的开关； 
                                     // Env.CmdEcho = true;//是否与lisp的回显；
                Env.OSMode = Env.OSModeType.Middle | Env.OSModeType.Pedal;//对像捕捉的设置，可以多加“|”符号；前面就是设置中点与垂足
                                                                          //currentSpace是当前打开的空间：比如块空间里等等，一般用这个来添加实体；    
                Env.Editor.Redraw();//刷新、重绘(里面输入重绘的图元等)

                //
                using var tr = new DBTrans();

                tr.LayerTable.Add("1");//在当前文档中加一个名为“1”的图层；
                tr.LayerTable.Remove("1");//删除文档中名为“1”的图层； 
                tr.LayerTable.Rename("1", "2");//修改文档中，名为“1”的图层名为“2”；
                                               //图层改色的方法1
                if (tr.LayerTable.Has("1"))//先判断一下是不是有“1”这个图层； 
                {
                    tr.LayerTable.Change("1", ltr => //对图层进行修改，也可以进行委托方法进行； 
                    {
                        ltr.Color = Color.FromColorIndex(ColorMethod.ByColor, 2);//进行图层颜色的修改；
                    });
                }
                //图层改色的方法2
                if (tr.LayerTable.Has("1"))//先判断一下是不是有“1”这个图层； 
                {
                    LayerTableRecord? ltr = tr.LayerTable.GetRecord("1", OpenMode.ForWrite);//拿到图层表记录给到ltr变量
                    ltr.Color = Color.FromColorIndex(ColorMethod.ByColor, 2);//再进行图层颜色的修改；

                }
                var trueOrflase = tr.LayerTable.Has("1");//判断当前文档中是否有名为“1”的图层，如何有，返回ture； 
                tr.LayerTable.Add("1", ltr => //这个是对图层“1”的一个委托方法，可以一次多个设置；
                {
                    ltr.IsOff = true;//图层打开与关闭；
                    ltr.IsPlottable = true;//图层是可打印与不可打印； 
                    ltr.IsLocked = true;//图层是否是锁定；
                    ltr.IsReconciled = true;//图层是不是可协调；
                });
                //遍历
                tr.LayerTable.ForEach(ltr => //委托的方法，在当前打开的文档中查找 ltr的对像
                {
                    if (ltr.Color.ColorIndex == 1) //如果有“1”为红色的这个图层；
                    {
                        using (ltr.ForWrite())//开启事务
                        {
                            ltr.Erase();//抹除=删除这个图层；
                        }
                    }
                });
                #region 画一条线写入到块表里； 
                var userLine = Env.Editor.GetPoint("\n请选第一点： ");//用户选一个点
                if (userLine.Status != PromptStatus.OK)
                    return;
                var pt1 = userLine.Value.Ucs2Wcs();//Wcs坐标改为世界坐标UCS； 
                var lineId = tr.BlockTable.Add("userBlock", ubk => //tr创建了一个块，再用委托的方法画线
                {
                    //Point3d.Origin是块内的坐标原点，这个原点是鼠标的指定点； 再指定线的另一点
                    ubk.AddEntity(new Line(Point3d.Origin, new Point3d(100, 100, 0)));
                });
                //方法1：把用户选的点写入委托方法画的点的块表里； insertBlock是IFox提供的插入块的方法；
                tr.CurrentSpace.InsertBlock(pt1, lineId);
                //方法2：用传统的方法写有另一个好处是可以对块再编辑：给块设定颜色等等
                var brf = new BlockReference(pt1, lineId)
                {
                    ColorIndex = 1,
                };
                tr.CurrentSpace.AddEntity(brf);//把brf这个块写入当前的空间里；
                #endregion

                #region 拖拽类插入块
                //绘制 一个块，名aaa，用委托的方法，设置这个块是由块内原点（Origin）到100，100之间的线；
                //IFOX如果用Add操作符号表时，有同名时，就直接返回id，没有就新建这个名的图块或图层等等；
                var id = tr.BlockTable.Add("aaa", btr =>
                {
                    btr.AddEntity(new Line(Point3d.Origin, new Point3d(100, 100, 0)));
                });
                //拿到这个名为“id”的块，做为参照来用，指定鼠标点为块内原点；
                var brfm = new BlockReference(Point3d.Origin, id);
                //用IFox的拖动类来实现拖动；
                using var j1 = new JigEx((mpw, _) =>
                {
                    brfm.Position = mpw;
                });
                j1.DatabaseEntityDraw(wd => wd.Geometry.Draw(brfm));//对j1进行重绘；
                j1.SetOptions("\n选择块的插入位置：");//提示语；
                var r1 = Env.Editor.Drag(j1);
                if (r1.Status != PromptStatus.OK)
                    return;
                tr.CurrentSpace.AddEntity(brfm);
                Env.Editor.Redraw(brfm);//刷新、重绘(里面输入重绘的图元等
                #endregion
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("打开工具失败！");
                Env.Editor.WriteMessage(ex.Message);
            }
        }

        #endregion


        #endregion

        #region 方向按键
        // 1.75 = 顺时针45    
        // 1.5  = 顺时针90    
        // 1.25 = 顺时针135   
        // 1    =       180
        // 0.75 = 逆时针135
        // 0.5  = 逆时针90
        // 0.25 = 逆时针45
        public void button_向下_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("下");
            command?.Invoke();
        }

        public void button_向左下_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("左下");
            command?.Invoke();
        }

        public void button_向左_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("左");
            command?.Invoke();
        }

        public void button_向左上_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("左上");
            command?.Invoke();
        }

        public void button_向上_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("上");
            command?.Invoke();

        }

        public void button_向右上_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("右上");
            command?.Invoke();
        }

        public void button_向右_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("右");
            command?.Invoke();
        }

        public void button_向右下_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("右下");
            command?.Invoke();
        }
        #endregion

        #region 检查专业图元
        /// <summary>
        /// 查检区域填充图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_onOff_QY_Layer_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button_onOff_QY_Layer.ForeColor.Name == "Black" || button_onOff_QY_Layer.ForeColor.Name == "ControlText")
            {
                button_onOff_QY_Layer.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_onOff_QY_Layer.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();

            VariableDictionary.selectTjtLayer.Add("QY");
            VariableDictionary.selectTjtLayer.Add("qy");

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }
        /// <summary>
        /// 检查设备图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_SB_onOff_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button_SB_onOff.ForeColor.Name == "Black" || button_SB_onOff.ForeColor.Name == "ControlText")
            {
                button_SB_onOff.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_SB_onOff.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.allTjtLayer.Clear();
            VariableDictionary.selectTjtLayer.Add("1");
            VariableDictionary.selectTjtLayer.Add("SB");
            VariableDictionary.selectTjtLayer.Add("SB(设备名称)");
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }
        /// <summary>
        /// 检查工艺条件按键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_GY_检查工艺_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button_检查工艺.ForeColor.Name == "Black" || button_检查工艺.ForeColor.Name == "ControlText")
            {
                button_检查工艺.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_检查工艺.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("CloseAllLayer ", false, false, false);

        }
        /// <summary>
        /// 关闭工艺图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_GY_关闭工艺_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtn;
            if (button_关闭工艺.ForeColor.Name == "Black" || button_关闭工艺.ForeColor.Name == "ControlText")
            {
                button_关闭工艺.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button_关闭工艺.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.allTjtLayer.Clear();
            foreach (var item in VariableDictionary.tjtBtn)
            {
                VariableDictionary.allTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("OpenLayer ", false, false, false);

        }
        /// <summary>
        /// 关闭工艺外的图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_GY_保留工艺_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            if (button_保留工艺.ForeColor.Name == "Black" || button_保留工艺.ForeColor.Name == "ControlText")
            {
                button_保留工艺.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_保留工艺.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.tjtBtn)
            {
                VariableDictionary.allTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);
        }
        /// <summary>
        /// 保留建筑，关闭其它专业图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_JZ_保留建筑_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.AtjtBtn;
            if (button_保留建筑.ForeColor.Name == "Black" || button_保留建筑.ForeColor.Name == "ControlText")
            {
                button_保留建筑.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_保留建筑.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }

            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.AtjtBtn)
            {
                VariableDictionary.allTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);

        }
        /// <summary>
        /// 关闭建筑打开其它专业图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_JZ_关闭建筑_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.AtjtBtn;
            if (button_关闭建筑.ForeColor.Name == "Black" || button_关闭建筑.ForeColor.Name == "ControlText")
            {
                button_关闭建筑.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button_关闭建筑.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.allTjtLayer.Clear();
            foreach (var item in VariableDictionary.AtjtBtn)
            {
                VariableDictionary.allTjtLayer.Add(item);
            }
            //Env.Document.SendStringToExecute("OpenLayer ", false, false, false);
            Env.Document.SendStringToExecute("ToggleLayerDeletion ", false, false, false);
        }
        /// <summary>
        /// 关闭其它
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_JZ_检查建筑_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.AtjtBtn;
            if (button_检查建筑条件.ForeColor.Name == "Black" || button_检查建筑条件.ForeColor.Name == "ControlText")
            {
                button_检查建筑条件.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_检查建筑条件.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.AtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("CloseAllLayer ", false, false, false);
        }
        /// <summary>
        /// 保留结构关闭其它专业图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_检查结构_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.StjtBtn;
            if (button_检查结构.ForeColor.Name == "Black" || button_检查结构.ForeColor.Name == "ControlText")
            {
                button_检查结构.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_检查结构.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.StjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("CloseAllLayer ", false, false, false);
        }
        /// <summary>
        /// 关闭结构图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_关闭结构_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.StjtBtn;
            if (button_关闭结构.ForeColor.Name == "Black" || button_关闭结构.ForeColor.Name == "ControlText")
            {
                button_关闭结构.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button_关闭结构.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.allTjtLayer.Clear();
            foreach (var item in VariableDictionary.StjtBtn)
            {
                VariableDictionary.allTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("OpenLayer ", false, false, false);
        }
        /// <summary>
        /// 保留结构图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_保留结构_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.StjtBtn;
            if (button_保留结构.ForeColor.Name == "Black" || button_保留结构.ForeColor.Name == "ControlText")
            {
                button_保留结构.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_保留结构.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.StjtBtn)
            {
                VariableDictionary.allTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);

        }
        /// <summary>
        /// 检查给排水
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_P_检查给排水_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.PtjtBtn;

            if (button_检查给排水.ForeColor.Name == "Black" || button_检查给排水.ForeColor.Name == "ControlText")
            {
                button_检查给排水.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_检查给排水.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.PtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("CloseAllLayer ", false, false, false);
        }
        public void button_P_关闭给排水_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.PtjtBtn;
            if (button_关闭给排水.ForeColor.Name == "Black" || button_关闭给排水.ForeColor.Name == "ControlText")
            {
                button_关闭给排水.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button_关闭给排水.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.allTjtLayer.Clear();
            foreach (var item in VariableDictionary.PtjtBtn)
            {
                VariableDictionary.allTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("OpenLayer ", false, false, false);
        }
        public void button_P_保留给排水_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.PtjtBtn;

            if (button_保留给排水.ForeColor.Name == "Black" || button_保留给排水.ForeColor.Name == "ControlText")
            {
                button_保留给排水.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_保留给排水.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                VariableDictionary.btnState = false;
            }
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.PtjtBtn)
            {
                VariableDictionary.allTjtLayer.Remove(item);
            }

            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);
        }
        public void button_NT_检查暖通_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.NtjtBtn;

            if (button_检查暖通.ForeColor.Name == "Black" || button_检查暖通.ForeColor.Name == "ControlText")
            {
                button_检查暖通.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_检查暖通.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.NtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("CloseAllLayer ", false, false, false);

        }
        public void button_NT_关闭暖通_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.NtjtBtn;
            if (button_关闭暖通.ForeColor.Name == "Black" || button_关闭暖通.ForeColor.Name == "ControlText")
            {
                button_关闭暖通.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button_关闭暖通.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.allTjtLayer.Clear();
            foreach (var item in VariableDictionary.NtjtBtn)
            {
                VariableDictionary.allTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("OpenLayer ", false, false, false);
        }
        public void button_NT_保留暖通_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.NtjtBtn;
            if (button_保留暖通.ForeColor.Name == "Black" || button_保留暖通.ForeColor.Name == "ControlText")
            {
                button_保留暖通.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_保留暖通.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.NtjtBtn)
            {
                VariableDictionary.allTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);

        }
        /// <summary>
        /// 只保留电气，关闭其它所有图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_E_检查电气_Click(object sender, EventArgs e)
        {
            if (button_检查电气.ForeColor.Name == "Black" || button_检查电气.ForeColor.Name == "ControlText")
            {
                button_检查电气.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_检查电气.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.EtjtBtn)
            {
                //if (!VariableDictionary.EtjtBtn.Contains(item))
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("CloseAllLayer ", false, false, false);
        }
        /// <summary>
        /// 只关闭电气，保留其它所有图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_E_关闭电气_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.EtjtBtn;
            if (button_关闭电气.ForeColor.Name == "Black" || button_关闭电气.ForeColor.Name == "ControlText")
            {
                button_关闭电气.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button_关闭电气.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.allTjtLayer.Clear();
            foreach (var item in VariableDictionary.EtjtBtn)
            {
                VariableDictionary.allTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("OpenLayer ", false, false, false);
        }
        /// <summary>
        /// 只保留电气，关闭其它条件图层，但不是条件的图层不改变状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_E_保留电气_Click(object sender, EventArgs e)
        {
            if (!VariableDictionary.btnState && VariableDictionary.tjtBtn != VariableDictionary.tjtBtnNull)
                VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.EtjtBtn;
            if (button_保留电气.ForeColor.Name == "Black" || button_保留电气.ForeColor.Name == "ControlText")
            {
                button_保留电气.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_保留电气.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.EtjtBtn)
            {
                VariableDictionary.allTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);
        }
        public void button_ZK_检查自控_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.ZKtjtBtn;

            if (button_检查自控.ForeColor.Name == "Black" || button_检查自控.ForeColor.Name == "ControlText")
            {
                button_检查自控.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_检查自控.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ZKtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("CloseAllLayer ", false, false, false);

        }
        public void button_ZK_关闭自控_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.ZKtjtBtn;
            if (button_关闭自控.ForeColor.Name == "Black" || button_关闭自控.ForeColor.Name == "ControlText")
            {
                button_关闭自控.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button_关闭自控.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.allTjtLayer.Clear();
            foreach (var item in VariableDictionary.ZKtjtBtn)
            {
                VariableDictionary.allTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("OpenLayer ", false, false, false);
        }
        public void button_ZK_保留自控_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            VariableDictionary.tjtBtn = VariableDictionary.ZKtjtBtn;

            if (button_保留自控.ForeColor.Name == "Black" || button_保留自控.ForeColor.Name == "ControlText")
            {
                button_保留自控.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                VariableDictionary.btnState = true;

            }
            else
            {
                button_保留自控.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                VariableDictionary.btnState = false;
            }
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ZKtjtBtn)
            {
                VariableDictionary.allTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);
        }

        //public void button关闭总图_Click(object sender, EventArgs e)
        //{
        //    for (int i = 0; i < VariableDictionary.tjtBtn .Length; i++)
        //    {
        //        VariableDictionary.tjtBtn [i] = "";
        //    }
        //    VariableDictionary.tjtBtn [0] = "STJ";
        //    Env.Document.SendStringToExecute("OpenLayer ", false, false, false);
        //}

        #endregion

        #region  共用条件
        public void button_共用条件说明_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "共用条件说明";
            VariableDictionary.btnBlockLayer = "TJ(共用条件)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 161;//设置为被插入的图层颜色
            Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
        }

        #endregion
        /// <summary>
        /// 通用处理按键命令
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="btnFileName"></param>
        /// <param name="btnBlockLayer"></param>
        /// <param name="layerColorIndex"></param>
        /// <param name="rotateAngle"></param>
        private void ExecuteProcessCommand(string commandName, string btnFileName, string btnBlockLayer, int layerColorIndex, double rotateAngle)
        {
            try
            {
                VariableDictionary.entityRotateAngle = rotateAngle;
                VariableDictionary.btnFileName = btnFileName;
                VariableDictionary.btnBlockLayer = btnBlockLayer;//设置为被插入的图层名
                VariableDictionary.layerColorIndex = layerColorIndex;//设置为被插入的图层颜色

                Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行命令时出错: {ex.Message}");
            }
        }


        #region 工艺
        public void button_GY_房间号_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("房间号", "RWS", "TJ(工艺专业GY)", 40, 0);
        }

        public void button_G_低温循环上水_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("低温循环上水", "RWS,DN??,??m³/h", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_低压蒸汽_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("低压蒸汽", "LS,DN??,??MPa,??kg/h,", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_二氧化碳_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("二氧化碳", "CO2,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_氮气_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("氮气", "N2,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_氧气_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("氧气", "O2,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_常温循环上水_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("常温循环上水", "CWS,DN??,??m³/h", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_注射用水_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("注射用水", "WFI,DN??,??℃,??L/h,使用量??L/h", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_纯蒸汽_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("纯蒸汽", "LS,DN??,??MPa,??kg/h,", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_纯化水_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("纯化水");
            command?.Invoke();
            //ExecuteProcessCommand("纯化水", "PW,DN??,??L/h", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_仪表压缩空气_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("仪表压缩空气", "IA,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_无菌压缩空气_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("无菌压缩空气", "CA,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_热水上水_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("热水上水", "HWS,DN??,??m³/h", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_凝结回水_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("凝结回水", "SC,DN??", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_液体物料_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("液体物料", "PL", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_乙二醇冷却上液_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("乙二醇冷却上液", "EGS,DN??,??m³/h", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_软化水_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("软化水", "SW,DN??,??m³/h", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_真空_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("真空", "VE", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_放空管_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("放空管", "VT", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_氨水_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("氨水", "AW", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_乙醇_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("乙醇", "AH", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_酸液_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("酸液", "AL", "TJ(工艺专业GY)", 40, 0);
        }
        public void button_G_碱液_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("碱液", "SL", "TJ(工艺专业GY)", 40, 0);
        }
        #endregion

        #region 工艺暖通

        public void button_NT_排潮_Click(object sender, EventArgs e)
        {

            ExecuteProcessCommand("排潮", "(排潮)", "TJ(暖通专业N)", 6, 0);
        }
        public void button_NT_排尘_Click(object sender, EventArgs e)
        {

            ExecuteProcessCommand("排尘", "(排尘)", "TJ(暖通专业N)", 6, 0);
        }
        public void button_NT_排热_Click(object sender, EventArgs e)
        {

            ExecuteProcessCommand("排热", "(排热)", "TJ(暖通专业N)", 6, 0);
        }
        public void button_NT_直排_Click(object sender, EventArgs e)
        {

            ExecuteProcessCommand("直排", "(直排)", "TJ(暖通专业N)", 6, 0);
        }
        public void button_NT_除味_Click(object sender, EventArgs e)
        {

            ExecuteProcessCommand("除味", "(除味)", "TJ(暖通专业N)", 6, 0);
        }
        public void button_NT_A级高度_Click(object sender, EventArgs e)
        {

            ExecuteProcessCommand("A级高度", "(A级高度？米)", "TJ(暖通专业N)", 6, 0);
        }
        public void button_NT_设备取风量_Click(object sender, EventArgs e)
        {

            ExecuteProcessCommand("设备取风量", "(设备取风量 ？m³/h)", "TJ(暖通专业N)", 6, 0);
        }
        public void button_NT_设备排风量_Click(object sender, EventArgs e)
        {

            ExecuteProcessCommand("设备排风量", "(设备排风量 ？m³/h)", "TJ(暖通专业N)", 6, 0);
        }
        public void button_排风百分比_Click(object sender, EventArgs e)
        {
            if (textBox_排风百分比.Text == "排风百分比")
            {
                VariableDictionary.btnFileName = "(排风 ？ %)";
            }
            else
            {
                VariableDictionary.btnFileName = "(排风 " + textBox_排风百分比.Text + " %)";
            }

            Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
            ExecuteProcessCommand("排风百分比", $"{VariableDictionary.btnFileName}", "TJ(暖通专业N)", 6, 0);

        }
        public void button_NT_温度_Click(object sender, EventArgs e)
        {

            ExecuteProcessCommand("温度", "(温度 ？℃±？℃)", "TJ(暖通专业N)", 6, 0);
        }
        public void button_NT_湿度_Click(object sender, EventArgs e)
        {
            ExecuteProcessCommand("湿度", "(湿度 ？%±？%)", "TJ(暖通专业N)", 6, 0);
        }
        #endregion

        #region 建筑
        public void button_JZ_吊顶_Click(object sender, EventArgs e)
        {
            VariableDictionary.winForm_Status =true;
            VariableDictionary.winFormDiaoDingHeight = textBox_吊顶高文字.Text;
            var command = UnifiedCommandManager.GetCommand("吊顶");
            command?.Invoke();
        }

        public void button_JZ_不吊顶_Click(object sender, EventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("不吊顶");
            command?.Invoke();
        }

        public void button_JZ_防撞护板_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "防撞护板";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(建筑专业J)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 30;//设置为被插入的图层颜色
            VariableDictionary.textbox_Gap = Convert.ToDouble(textBox_距离墙值.Text);
            Env.Document.SendStringToExecute("ParallelLines ", false, false, false);

        }
        public void button_冷藏库降板_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "冷藏库降板（270）";
            //VariableDictionary.btnBlockLayer = "GYTJ-碱液";
            VariableDictionary.btnBlockLayer = "TJ(建筑专业J)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 30;//设置为被插入的图层颜色
            Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
            var command = UnifiedCommandManager.GetCommand("冷藏库降板");
            command?.Invoke();
        }

        public void button_冷冻库降板_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "冷冻库降板（390）";
            //VariableDictionary.btnBlockLayer = "GYTJ-碱液";
            VariableDictionary.btnBlockLayer = "TJ(建筑专业J)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 30;//设置为被插入的图层颜色
            Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
            var command = UnifiedCommandManager.GetCommand("冷藏库降板");
            command?.Invoke();
        }
        public void button_排水沟_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "JZTJ_排水沟";
            VariableDictionary.buttonText = "JZTJ_排水沟";
            //VariableDictionary.btnFileName_blockName = "$TWTSYS$00000508";
            VariableDictionary.btnBlockLayer = "TJ(建筑专业J)";
            VariableDictionary.layerColorIndex = 30;//设置为被插入的图层颜色
            VariableDictionary.dimString_JZ_宽 = textBox_排水沟_宽.Text;
            VariableDictionary.dimString_JZ_深 = textBox_排水沟_深.Text;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            //VariableDictionary.resourcesFile = Resources.PTJ_消火栓;
            Env.Document.SendStringToExecute("Rec2PolyLine_3 ", false, false, false);
            var command = UnifiedCommandManager.GetCommand("排水沟");
            command?.Invoke();
        }
 
        public void button_JZ_房间号_Click(object sender, EventArgs e)
        {
            VariableDictionary.winForm_Status = true;
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = textBox_楼层号.Text + "-" + textBox_洁净区1_2.Text + textBox_系统分区.Text + textBox_房间号副号.Text;
            VariableDictionary.btnBlockLayer = "TJ(房间编号)";//设置为被插入的图层名

            if (textBox_楼层号.Text == "1" && textBox_洁净区1_2.Text == "1" && textBox_系统分区.Text == "1")
            {
                VariableDictionary.layerColorIndex = 64;
                VariableDictionary.jjqInt = 1;
                VariableDictionary.xtqInt = 1;
            }
            else if (Convert.ToInt32(textBox_洁净区1_2.Text) != VariableDictionary.jjqInt)
            {
                var layerColorTest = VariableDictionary.jjqLayerColorIndex[Convert.ToInt32(textBox_洁净区1_2.Text)];
                VariableDictionary.layerColorIndex = Convert.ToInt16(layerColorTest);//设置为被插入的图层颜色
                VariableDictionary.jjqInt = Convert.ToInt32(textBox_洁净区1_2.Text);
            }
            else if (Convert.ToInt32(textBox_系统分区.Text) != VariableDictionary.xtqInt)
            {
                var layerColorTest = VariableDictionary.xtqLayerColorIndex[Convert.ToInt32(textBox_系统分区.Text)];
                VariableDictionary.layerColorIndex = Convert.ToInt16(layerColorTest);//设置为被插入的图层颜色
                VariableDictionary.xtqInt = Convert.ToInt32(textBox_系统分区.Text);
            }
            Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
            if (Convert.ToInt32(textBox_房间号副号.Text) < 9)
            {
                textBox_房间号副号.Text = "0" + (Convert.ToInt32(textBox_房间号副号.Text) + 1).ToString();
            }
            else
            {
                textBox_房间号副号.Text = (Convert.ToInt32(textBox_房间号副号.Text) + 1).ToString();
            }
            
            var command = UnifiedCommandManager.GetCommand("房间编号");
            command?.Invoke();
        }
        #endregion

        #region 自控
        public void button_ZK_无线AP_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_无线AP";
            VariableDictionary.btnFileName_blockName = "$equip$00001857";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = GB_NewCadPlus_III.Resources.ZKTJ_EQUIP_无线AP;
            VariableDictionary.blockScale = 500;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }
        public void button_ZK_电话插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_电话插座";
            VariableDictionary.btnFileName_blockName = "$equip$00001867";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.blockScale = 500;
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_电话插座;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }
        public void button_ZK_网络插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_网络插座";
            VariableDictionary.btnFileName_blockName = "$equip$00001847";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.blockScale = 500;
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_网络插座;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }
        public void button_ZK_电话网络插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_电话网络插座";
            VariableDictionary.btnFileName_blockName = "ZKTJ-电话网络插座";
            // VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.blockScale = 500;
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_电话网络插座;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }
        public void button_ZK_安防监控_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_安防监控";
            VariableDictionary.btnFileName_blockName = "HC002695005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.blockScale = 500;
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_安防监控;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }
        public void button_ZK_眼纹识别器_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_眼纹识别器";
            VariableDictionary.btnFileName_blockName = "$equip$00002616";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_眼纹识别器;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }
        public void button_ZK_无线网络接入点_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_无线AP";
            VariableDictionary.btnFileName_blockName = "$equip$00003217";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_无线AP;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }
        public void button_ZK_室外彩色云台摄像机_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_室外彩色云台摄像机";
            VariableDictionary.btnFileName_blockName = "$equip$00002970";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_室外彩色云台摄像机;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }
        public void button_ZK_外线电话插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_外线电话插座";
            VariableDictionary.btnFileName_blockName = "$Equip$00003196";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_外线电话插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_网络交换机_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_网络交换机";
            VariableDictionary.btnFileName_blockName = "$equip$00002332";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_网络交换机;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_室外彩色摄像机_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_室外彩色摄像机";
            VariableDictionary.btnFileName_blockName = "$equip$00002969";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_室外彩色摄像机;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_人像识别器_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_人像识别器";
            VariableDictionary.btnFileName_blockName = "$equip$00002496";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_人像识别器;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_内线电话插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_内线电话插座";
            VariableDictionary.btnFileName_blockName = "$Equip$00003195";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_内线电话插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_门磁开关_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_门磁开关";
            VariableDictionary.btnFileName_blockName = "$equip$00002621";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_门磁开关;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_局域网插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_局域网插座";
            VariableDictionary.btnFileName_blockName = "$Equip$00003198";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_局域网插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_门禁控制器_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_门禁控制器";
            VariableDictionary.btnFileName_blockName = "$equip_U$00000028";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_门禁控制器;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_读卡器_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_读卡器";
            VariableDictionary.btnFileName_blockName = "$equip$00002617";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_读卡器;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_带扬声器电话机_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_带扬声器电话机";
            VariableDictionary.btnFileName_blockName = "$equip$00003042";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_带扬声器电话机;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_互联网插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_互联网插座";
            VariableDictionary.btnFileName_blockName = "$Equip$00003197";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_互联网插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_广角彩色摄像机_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_广角彩色摄像机";
            VariableDictionary.btnFileName_blockName = "$equip$00002731";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_广角彩色摄像机;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_防爆型网络摄像机_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_防爆型网络摄像机";
            VariableDictionary.btnFileName_blockName = "$equip$00002975";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_防爆型网络摄像机;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_防爆型电话机_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_防爆型电话机";
            VariableDictionary.btnFileName_blockName = "$equip$00003047";
            // VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-通讯";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_防爆型电话机;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_半球彩色摄像机_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_半球彩色摄像机";
            VariableDictionary.btnFileName_blockName = "$equip$00002353";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_半球彩色摄像机;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }

        public void button_ZK_电锁按键_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_电锁按键";
            VariableDictionary.btnFileName_blockName = "$equip$00002375";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_电锁按键;
            VariableDictionary.blockScale = 500;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_ZK_电控锁_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_电控锁";
            VariableDictionary.btnFileName_blockName = "$equip$00002474";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_电控锁;
            VariableDictionary.blockScale = 500;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }

        public void button_ZK_监控文字_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "ZKTJ_EQUIP_监控文字";
            VariableDictionary.btnFileName_blockName = "ZKTJ-EQUIP-监控";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "EQUIP-安防";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 3;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.ZKTJ_EQUIP_监控文字;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        #endregion

        #region 结构
        /// <summary>
        /// 结构插入受力点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_结构受力点_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "TJ(结构专业JG)";
            VariableDictionary.btnFileName_blockName = "A$C9bff4efc";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;//设置为被插入的图层名
            VariableDictionary.buttonText = "STJ_受力点";
            VariableDictionary.layerColorIndex = 231;//设置为被插入的图层颜色\
            VariableDictionary.dimString = textBox_荷载数据.Text;
            VariableDictionary.resourcesFile = Resources.STJ_受力点;
            VariableDictionary.blockScale = 1;
            Env.Document.SendStringToExecute("GB_InsertBlock_5 ", false, false, false);
        }
        /// <summary>
        /// 结构水平荷载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_水平荷载_Click(object sender, EventArgs e)
        {
            //VariableDictionary.entityRotateAngle = 0;textBox_荷载数据
            VariableDictionary.btnFileName = "TJ(结构专业JG)";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.buttonText = "STJ_水平荷载";
            VariableDictionary.dimString = textBox_荷载数据.Text;
            VariableDictionary.layerColorIndex = 231;
            //VariableDictionary.resourcesFile = Resources.STJ_水平荷载;
            Env.Document.SendStringToExecute("NLinePolyline_N ", false, false, false);
        }

        /// <summary>
        /// 面着地   DrawPolyline
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_面着地_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(结构专业JG)";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.buttonText = "STJ_面着地";
            VariableDictionary.dimString = textBox_荷载数据.Text;
            VariableDictionary.layerColorIndex = 231;
            Env.Document.SendStringToExecute("NLinePolyline ", false, false, false);
        }

        /// <summary>
        /// 结构框着地
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_框着地_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(结构专业JG)";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.buttonText = "STJ_框着地";
            VariableDictionary.layerColorIndex = 231;
            VariableDictionary.dimString = textBox_荷载数据.Text;
            Env.Document.SendStringToExecute("NLinePolyline_Not ", false, false, false);
        }


        /// <summary>
        /// 结构直径圆形开洞
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_圆形开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(结构专业JG)";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.buttonText = "STJ_圆形开洞";
            //VariableDictionary.btnBlockLayer = "TJ(结构专业JG)";
            VariableDictionary.textBox_S_CirDiameter = Convert.ToDouble(textBox_S_直径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_cirDiameter_Plus.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 231;
            if (VariableDictionary.textBox_S_CirDiameter == 0)
            {
                Env.Document.SendStringToExecute("CirDiameter ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirDiameter_2 ", false, false, false);
            }
            ;
        }
        /// <summary>
        /// 结构半径开圆洞口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_半径开圆洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(结构专业JG)";
            VariableDictionary.buttonText = "STJ_圆形开洞";
            //VariableDictionary.btnBlockLayer = "TJ(结构专业JG)";
            VariableDictionary.textbox_S_Cirradius = Convert.ToDouble(textBox_S_半径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_cirRadius_Plus.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 231;
            if (VariableDictionary.textbox_S_Cirradius == 0)
            {
                Env.Document.SendStringToExecute("CirRadius ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirRadius_2 ", false, false, false);
            }
            ;
        }
        /// <summary>
        /// 矩形开洞
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_S_矩形开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.textbox_Height = textBox_S_高.Text;
            VariableDictionary.textbox_Width = textBox_S_宽.Text;
            VariableDictionary.btnBlockLayer = "TJ(结构专业JG)";
            VariableDictionary.buttonText = "矩形开洞";
            VariableDictionary.layerColorIndex = 231;
            if (Convert.ToDouble(VariableDictionary.textbox_Height) > 0 && Convert.ToDouble(VariableDictionary.textbox_Width) > 0)
            {
                recAndMRec = 0;
                VariableDictionary.btnFileName = "TJ(结构专业JG)";

                VariableDictionary.textbox_RecPlus_Text = textBox2_RectangleExpansion.Text;
                Env.Document.SendStringToExecute("DrawRec ", false, false, false);
            }
            else
            {
                VariableDictionary.btnFileName = "TJ(结构专业JG)";
                VariableDictionary.textbox_RecPlus_Text = textBox2_RectangleExpansion.Text;

                Env.Document.SendStringToExecute("Rec2PolyLine ", false, false, false);
                //Env.Editor.Regen();
            }



        }
        ///// <summary>
        ///// 矩形开洞
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //public void button_S_矩形开洞_Click(object sender, EventArgs e)
        //{
        //    VariableDictionary.buttonText = "STJ_矩形开洞";
        //    VariableDictionary.textbox_RecPlus_Text = textBox2_RectangleExpansion.Text;
        //    Env.Document.SendStringToExecute("Rec2PolyLine ", false, false, false);
        //    Env.Editor.Regen();
        //}

        //Rec2PolyLine_N

        /// <summary>
        /// 工艺内结构画矩形为0，结构内画矩形为1
        /// </summary>
        public static int recAndMRec = 0;
        /// <summary>
        /// 结构指定长宽画矩形
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_Rectangle_Click(object sender, EventArgs e)
        {
            recAndMRec = 1;
            VariableDictionary.textbox_Height = textBox2_height.Text;
            VariableDictionary.textbox_Width = textBox2_width.Text;
            VariableDictionary.buttonText = "TJ(结构洞口)";
            VariableDictionary.btnBlockLayer = "TJ(结构洞口)";
            VariableDictionary.buttonText = "PTJ_矩形开洞";
            VariableDictionary.layerColorIndex = 231;
            VariableDictionary.btnFileName = "TJ(结构洞口)";
            VariableDictionary.textbox_RecPlus_Text = "0";
            Env.Document.SendStringToExecute("DrawRec ", false, false, false);
        }
        public void button_MRectangle_Click(object sender, EventArgs e)
        {
            recAndMRec = 0;
            VariableDictionary.textbox_Height = textBox_S_高.Text;
            VariableDictionary.textbox_Width = textBox_S_宽.Text;
            VariableDictionary.buttonText = "TJ(结构专业JG)";
            Env.Document.SendStringToExecute("DrawRec ", false, false, false);
        }

        #endregion

        #region 给排水

        public void button_P_洗眼器_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_洗眼器";
            VariableDictionary.btnFileName_blockName = "$TWTSYS$00000604";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色

            VariableDictionary.resourcesFile = Resources.PTJ_洗眼器;


            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }

        public void button_P_不给饮用水_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "不给饮用水";
            //VariableDictionary.btnFileName_blockName = "$TWTSYS$00000508";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.layerColorIndex = 7;//设置为被插入的图层颜色
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            //VariableDictionary.resourcesFile = Resources.PTJ_消火栓;
            Env.Document.SendStringToExecute("DDimLinearP ", false, false, false);
        }



        public void button_P_小便器给水_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_小便器给水";
            VariableDictionary.btnFileName_blockName = "$TWTSYS$00000603";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_小便器给水;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }

        public void button_P_大洗涤池_Click_1(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_大洗涤池";
            VariableDictionary.btnFileName_blockName = "$equip$00003217";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_大洗涤池_1x0_5m;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }

        public void button_P_大便器给水_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_大便器给水";
            VariableDictionary.btnFileName_blockName = "$TWTSYS$00000602";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_大便器给水;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }



        public void button_P_洗涤盆_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_洗涤盆";
            VariableDictionary.btnFileName_blockName = "普通区洗涤盆";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.TCH_Ptj_No = 0;
            // VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_洗涤盆;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_水池给水_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_水池给水";
            VariableDictionary.btnFileName_blockName = "$TWTSYS$00000605";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.blockScale = 1.5;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_水池给水;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }

        public void button_P_热直排管_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_热直排管";
            VariableDictionary.btnFileName_blockName = "$TwtSys$00000328";
            VariableDictionary.btnBlockLayer = "EQUIP_地漏";
            VariableDictionary.blockScale = 1.5;
            VariableDictionary.TCH_Ptj_No = 0;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_热直排管;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_冷直排管_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_冷直排管";
            VariableDictionary.btnFileName_blockName = "$TwtSys$00000327";
            VariableDictionary.btnBlockLayer = "EQUIP_地漏";
            VariableDictionary.blockScale = 1.5;
            VariableDictionary.TCH_Ptj_No = 0;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_冷直排管;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
            //Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }

        public void button_P_地漏_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_地漏";
            VariableDictionary.btnFileName_blockName = "$TwtSys$00000141";
            VariableDictionary.btnBlockLayer = "PTJ_地漏";
            VariableDictionary.blockScale = 1.5;
            VariableDictionary.TCH_Ptj_No = 0;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_地漏;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_给水点_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_给水点";
            VariableDictionary.btnFileName_blockName = "普通区给水";
            VariableDictionary.btnBlockLayer = "EQUIP_给水";
            VariableDictionary.blockScale = 1;
            VariableDictionary.TCH_Ptj_No = 0;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_给水点;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_洗脸盆_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_洗脸盆";
            //VariableDictionary.btnFileName_blockName = "$TWTSYS$00000600";
            VariableDictionary.btnFileName_blockName = "普通区洗脸盆";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.TCH_Ptj_No = 10;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_洗脸盆;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_冷不带压直排_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_冷不带压直排";
            VariableDictionary.btnFileName_blockName = "$TWTSYS$00000622";
            VariableDictionary.btnBlockLayer = "EQUIP_地漏";
            VariableDictionary.blockScale = 1;
            VariableDictionary.TCH_Ptj_No = 0;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_冷不带压直排;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_热不带压直排_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_热不带压直排";
            VariableDictionary.btnFileName_blockName = "$TwtSys$00000138";
            VariableDictionary.btnBlockLayer = "EQUIP_地漏";
            VariableDictionary.blockScale = 1;
            //TCH_Ptj = true;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_热不带压直排;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_拖布池_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_拖布池";
            VariableDictionary.btnFileName_blockName = "A$C32361FA1";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.TCH_Ptj_No = 0;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_拖布池;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_洗涤池1x05m_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_大洗涤池_1x0_5m";
            VariableDictionary.btnFileName_blockName = "A$C5C905366";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.TCH_Ptj_No = 10;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_大洗涤池_1x0_5m;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_洗涤池12x05m_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_大洗涤池_1_2x0_5m";
            VariableDictionary.btnFileName_blockName = "A$C18325CD1";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.TCH_Ptj_No = 10;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_大洗涤池_1_2x0_5m;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_洗涤池15x05m_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_大洗涤池_1_5x0_5m";
            VariableDictionary.btnFileName_blockName = "A$C5A6D4801";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.TCH_Ptj_No = 10;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_大洗涤池_1_5x0_5m;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_洗涤池18x05m_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_大洗涤池_1_8x0_5m";
            VariableDictionary.btnFileName_blockName = "A$C3C1E07D8";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.TCH_Ptj_No = 0;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_大洗涤池_1_8x0_5m;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        public void button_P_洗涤池2x05m_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "PTJ_大洗涤池_2_0x0_5m";
            VariableDictionary.btnFileName_blockName = "A$C0AB663B7";
            VariableDictionary.btnBlockLayer = "TJ(给排水专业S)";
            VariableDictionary.TCH_Ptj_No = 10;
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.PTJ_大洗涤池_2_0x0_5m;
            Env.Document.SendStringToExecute("GB_InsertBlock_Ptj ", false, false, false);
        }

        #endregion

        #region 电气
        public void button_DQ_220V插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相插座";
            VariableDictionary.btnFileName_blockName = "HC002694005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_三相380V插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_三相380V插座";
            VariableDictionary.btnFileName_blockName = "HC002696005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_三相380V插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_潮湿插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_潮湿插座";
            VariableDictionary.btnFileName_blockName = "HC002695005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_潮湿插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_三相潮湿插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_三相潮湿插座";
            VariableDictionary.btnFileName_blockName = "HC002697005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_三相潮湿插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_空调插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_空调插座";
            VariableDictionary.btnFileName_blockName = "HC003131100042";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_空调插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_设备用电点位_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_设备用电点位";
            VariableDictionary.btnFileName_blockName = "HC002694005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_设备用电点位;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相夹层_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相夹层插座";
            VariableDictionary.btnFileName_blockName = "HC002698005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相夹层插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_插座箱_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_插座箱";
            VariableDictionary.btnFileName_blockName = "DQTJ-插座箱";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.blockScale = 1;
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_插座箱;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_应急插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_应急插座";
            VariableDictionary.btnFileName_blockName = "HC002997005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D1)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 4;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_应急插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_应急16A电源_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_应急16A插座";
            VariableDictionary.btnFileName_blockName = "DQTJ-UPS16A电源";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D1)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 4;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_应急16A插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_UPS插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_UPS插座";
            VariableDictionary.btnFileName_blockName = "DQTJ-应急UPS电源";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D1)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 4;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_UPS插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_UPS16A插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_UPS16A插座";
            VariableDictionary.btnFileName_blockName = "DQTJ-应急UPS16A电源";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D1)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 4;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_UPS16A插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_传递窗电源插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_传递窗电源插座";
            VariableDictionary.btnFileName_blockName = "HC003001006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_传递窗电源插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_门禁插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_门禁插座";
            VariableDictionary.btnFileName_blockName = "A$C16EA1F35";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_门禁插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_红外感应门插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_红外感应门插座";
            VariableDictionary.btnFileName_blockName = "DQTJ-红外感应门插座";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_红外感应门插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_紫外灯_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "紫外灯";
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
        }
        public void button_DQ_三联插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_三联插座";
            VariableDictionary.btnFileName_blockName = "$equip$00001992";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.blockScale = 1.5;
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_三联插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_四联插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_四联插座";
            VariableDictionary.btnFileName_blockName = "$equip$00002163";
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_四联插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_互锁插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_互锁插座";
            VariableDictionary.btnFileName_blockName = "HC002698005707";
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_互锁插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_两点互锁_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_两点互锁";
            VariableDictionary.btnFileName_blockName = "DQTJ-两点互锁";
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.blockScale = 0.8;
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_两点互锁;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_三点互锁_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_三点互锁";
            VariableDictionary.btnFileName_blockName = "A$C0664bbbd";
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.blockScale = 0.8;
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_三点互锁;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_立式空调插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_立式空调插座";
            VariableDictionary.btnFileName_blockName = "HC003131000042";
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_立式空调插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_壁挂空调插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_壁挂空调插座";
            VariableDictionary.btnFileName_blockName = "HC003130000042";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_壁挂空调插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_手消毒插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_手消毒插座";
            VariableDictionary.btnFileName_blockName = "HC003007006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_手消毒插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_视孔灯_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_视孔灯";
            VariableDictionary.btnFileName_blockName = "$Equip$00003237";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_视孔灯;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_烘手器插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_烘手器插座";
            VariableDictionary.btnFileName_blockName = "A$C21791F4C";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_烘手器插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_实验台功能柱插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_实验台功能柱插座";
            VariableDictionary.btnFileName_blockName = "HC002694005706N";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名HC002694005706
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_实验台功能柱插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_实验台UPS功能柱插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_实验台UPS功能柱电源";
            VariableDictionary.btnFileName_blockName = "HC003210000042";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D1)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 4;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_实验台UPS功能柱电源;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_电热水器插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_电热水器安全型插座";
            VariableDictionary.btnFileName_blockName = "HC003021006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_电热水器安全型插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_厨宝插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_厨宝安全型插座";
            VariableDictionary.btnFileName_blockName = "A$C1E63194F";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_厨宝安全型插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_烘手器_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_烘手器";
            VariableDictionary.btnFileName_blockName = "$Equip$00003233";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_烘手器;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_驱鼠器插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_驱鼠器插座";
            VariableDictionary.btnFileName_blockName = "HC003076006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_驱鼠器插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_灭蝇灯插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_灭蝇灯插座";
            VariableDictionary.btnFileName_blockName = "HC003076006336";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_灭蝇灯插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_灭蝇灯插座_底边_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_灭蝇灯插座_底边";
            VariableDictionary.btnFileName_blockName = "HC002694005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_灭蝇灯插座_底边;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_实验台UPS功能柱电源_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_实验台UPS功能柱电源";
            VariableDictionary.btnFileName_blockName = "HC003210000042";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_实验台UPS功能柱电源;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_实验台上方220V插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_实验台上方220V插座";
            VariableDictionary.btnFileName_blockName = "HC003212000042";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_实验台上方220V插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }




        public void button_DQ_380V用电设备_点或配电柜_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_380V用电设备点或配电柜";
            VariableDictionary.btnFileName_blockName = "380V用电设备点或配电柜";
            VariableDictionary.dimString = textBox_E_input10KW.Text + "kW";
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_380V用电设备点或配电柜;
            Env.Document.SendStringToExecute("GB_InsertBlock_3 ", false, false, false);
        }
        public void button_DQ_380V用电设备大于10KW_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_380V用电设备大于10KW";
            VariableDictionary.btnFileName_blockName = "380V用电设备大于10KW";
            VariableDictionary.dimString = textBox_E_input10KW.Text + "kW";
            VariableDictionary.btnBlockLayer = "TJ(电气专业D2)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 110;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_380V用电设备大于10KW;
            Env.Document.SendStringToExecute("GB_InsertBlock_3 ", false, false, false);
        }
        public void button_DQ_220V用电设备_点或配电柜_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_220V用电设备点或配电柜";
            VariableDictionary.btnFileName_blockName = "220V用电设备点或配电柜";
            VariableDictionary.dimString = "220V," + textBox_E_input10KW.Text + "kW";
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_220V用电设备点或配电柜;
            Env.Document.SendStringToExecute("GB_InsertBlock_3 ", false, false, false);
        }


        //GB_InsertBlock_2
        //GB_InsertBlock_3
        //GB_InsertBlock_4
        //GB_InsertBlock_5
        //GB_InsertBlock_6

        public void button_DQ_单相插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相插座";
            VariableDictionary.btnFileName_blockName = "HC002694005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相地面插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相地面插座";
            VariableDictionary.btnFileName_blockName = "HC003202000042";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相地面插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相三孔插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相三孔插座";
            VariableDictionary.btnFileName_blockName = "HC002696005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相三孔插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相空调插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相空调插座";
            VariableDictionary.btnFileName_blockName = "HC003130000042";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相空调插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相16A三孔插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相16A三孔插座";
            VariableDictionary.btnFileName_blockName = "HC002805006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相16A三孔插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相20A三孔插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相20A三孔插座";
            VariableDictionary.btnFileName_blockName = "HC002944006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相20A三孔插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相25A三孔插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相25A三孔插座";
            VariableDictionary.btnFileName_blockName = "HC002806006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相25A三孔插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相32A三孔_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相32A三孔插座";
            VariableDictionary.btnFileName_blockName = "HC002957006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相32A三孔插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相五孔岛型插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相五孔岛型插座";
            VariableDictionary.btnFileName_blockName = "$equip_U$00000168";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相五孔岛型插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相三孔岛型插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相三孔岛型插座";
            VariableDictionary.btnFileName_blockName = "$equip_U$00000169";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相三孔岛型插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_三相岛型插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_三相岛型插座";
            VariableDictionary.btnFileName_blockName = "$equip_U$00000167";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_三相岛型插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_带保护极的单相防爆插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_带保护极的单相防爆插座";
            VariableDictionary.btnFileName_blockName = "HC002820006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_带保护极的单相防爆插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_带保护极的三相防爆插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_带保护极的三相防爆插座";
            VariableDictionary.btnFileName_blockName = "HC002821006335";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_带保护极的三相防爆插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相防爆岛型插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_单相防爆岛型插座";
            VariableDictionary.btnFileName_blockName = "$equip_U$00000170";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_单相防爆岛型插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相暗敷插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_带保护极的单相暗敷插座";
            VariableDictionary.btnFileName_blockName = "DQTJ-带保护极的单相暗敷插座";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName; 
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_带保护极的单相暗敷插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_单相密闭插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_带保护极的单相密闭插座";
            VariableDictionary.btnFileName_blockName = "HC002695005706";
            //VariableDictionary.btnFileName_blockName = "HC002697005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_带保护极的单相密闭插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_三相密闭插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_带保护极的三相密闭插座";
            VariableDictionary.btnFileName_blockName = "HC002697005706";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_带保护极的三相密闭插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }
        public void button_DQ_三相暗敷插座_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "DQTJ_EQUIP_带保护极的三相暗敷插座";
            VariableDictionary.btnFileName_blockName = "DQTJ-带保护极的三相暗敷插座";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.btnBlockLayer = "TJ(电气专业D)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.resourcesFile = Resources.DQTJ_EQUIP_带保护极的三相暗敷插座;
            Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
        }

        #endregion

        #region 外参图元

        public void button_获取外参图元_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("CopyAndSync1 ", false, false, false);
        }

        public void button获取外参图元2_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("ReferenceCopy ", false, false, false);
        }

        public void button获取外参图元三_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("CopyAndSync3 ", false, false, false);
        }

        public void button获取外参图元四_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("CopyAndSync6 ", false, false, false);
        }

        #endregion

        #region  外参相关按键、 选择外参并从中选图元复制到当前空间内
        /// <summary>
        /// 外参实体objectid列表
        /// </summary>
        private List<ObjectId> xrefEntities;
        /// <summary>
        /// 选中实体objectid列表
        /// </summary>
        public static List<ObjectId> selectedEntities = new List<ObjectId>();
        /// <summary>
        /// 选择的外参
        /// </summary>
        public static string? selectItem;
        /// <summary>
        /// 选择全部外参图元
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_获取外参全部图元_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("CopyXrefAllEntity ", false, false, false);
        }

        /// <summary>
        /// 选择外参并从中选图元列入列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_SelectReference_Click(object sender, EventArgs e)
        {
            #region
            ReferenceEntity.Items.Clear();
            SelectEntity.Items.Clear();
            Reference.Items.Clear();

            using var tr = new DBTrans();
            // 选择外部参照
            PromptEntityOptions opt = new PromptEntityOptions("选择一个外部参照：");
            opt.SetRejectMessage("您必须选择一个外部参照。");
            opt.AddAllowedClass(typeof(BlockReference), true);
            PromptEntityResult res = Env.Editor.GetEntity(opt);
            if (res.Status != PromptStatus.OK) return;
            // 获取外部参照中的图元
            xrefEntities = Command.GetXrefEntities(res.ObjectId);
            // 第五步：获取外部参照名称
            string xrefName = Command.getXrefName(tr, res.ObjectId);
            if (xrefName != null && xrefName != "")
                Reference.Items.Add(xrefName);
            // 添加图元到左侧列表
            foreach (ObjectId entityId in xrefEntities)
            {
                if (entityId != null)
                {
                    Entity entity = Command.GetEntity(entityId);
                    if (entity is not null)
                    {
                        selectedEntities.Add(entityId);
                        ReferenceEntity.Items.Add(Command.getXrefName(tr, entityId));
                    }
                }
            }
            tr.Commit();
            #endregion
            //Env.Document.SendStringToExecute("ReferenceCopy ", false, false, false);
            /// 发送命令到AutoCAD执行
            //Env.Document.SendStringToExecute("CopyXrefEntity2 ", false, false, false);

        }


        /// <summary>
        /// 选择实体，拿到这个实体的详细参数；
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void selectEntity(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("tzData ", false, false, false);
            Env.Editor.Redraw();
        }
        /// <summary>
        /// 引用实体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void referenceEntity_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBox listBox = (ListBox)sender;

            if (listBox.SelectedItem != null)
            {
                // 从左侧列表移除选中的图元并添加到右侧列表
                selectItem = listBox.SelectedItem.ToString();
                SelectEntity.Items.Add(selectItem);
                ReferenceEntity.Items.Remove(selectItem);
            }
        }
        /// <summary>
        /// 选择图元
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void selectEntity_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBox listBox = (ListBox)sender;

            if (listBox.SelectedItem != null)
            {
                // 从右侧列表移除选中的图元并添加到左侧列表
                selectItem = listBox.SelectedItem.ToString();
                ReferenceEntity.Items.Add(selectItem);
                SelectEntity.Items.Remove(selectItem);
            }
        }

        /// <summary>
        /// 选择外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Btn_selectEntity_Click(object sender, EventArgs e)
        {
            /// 发送命令到AutoCAD执行
            Env.Document.SendStringToExecute("CopyXrefEntity ", false, false, false);
        }

        public void button_ShowDatas_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("(vlax-dump-object (vlax-ename->vla-object (car (entsel )))T) ", false, false, false);
        }
        public void buttonTEST_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("CopyAtSamePosition ", false, false, false);
        }
        #endregion

        #region 开洞

        public void button_JZ_左右开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(建筑专业J)";
            VariableDictionary.layerColorIndex = 64;//设置图层颜色
            VariableDictionary.textbox_Width = textBoxA_左右开洞.Text;
            VariableDictionary.textbox_Height = "15";
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        public void button_JZ_上下开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(建筑专业J)";
            VariableDictionary.layerColorIndex = 64;//设置图层颜色
            VariableDictionary.textbox_Width = "15";
            VariableDictionary.textbox_Height = textBoxA_左右开洞.Text;
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        public void button_S_左右开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "HOLE";
            VariableDictionary.layerColorIndex = 231;//设置图层颜色
            VariableDictionary.textbox_Width = textBoxS_左右开洞.Text;
            VariableDictionary.textbox_Height = "15";
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        public void button_S_上下开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "HOLE";
            VariableDictionary.layerColorIndex = 231;//设置图层颜色
            VariableDictionary.textbox_Width = "15";
            VariableDictionary.textbox_Height = textBoxS_上下开洞.Text;
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        public void button_P_左右开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(给排水过建筑)";
            VariableDictionary.layerColorIndex = 7;//设置图层颜色
            VariableDictionary.textbox_Width = textBoxP_左右开洞.Text;
            VariableDictionary.textbox_Height = "15";
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        public void button_P_上下开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(给排水过建筑)";
            VariableDictionary.textbox_Width = "15";
            VariableDictionary.layerColorIndex = 7;//设置图层颜色
            VariableDictionary.textbox_Height = textBoxP_上下开洞.Text;
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        public void button_NT_左右开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(暖通过建筑)";//设置为被插入的图层名
            VariableDictionary.buttonText = "TJ(暖通过建筑)";
            VariableDictionary.layerColorIndex = 6;//设置图层颜色
            VariableDictionary.textbox_Width = textBoxN_左右开洞.Text;
            VariableDictionary.textbox_Height = "15";
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
        }

        public void button_NT_上下开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(暖通过建筑)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 6;//设置为被插入的图层颜色
            VariableDictionary.buttonText = "TJ(暖通过建筑)";
            VariableDictionary.textbox_Height = textBoxN_上下开洞.Text;
            VariableDictionary.textbox_Width = "15";
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
        }

        public void button_DQ_左右开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(电气过建筑孔洞D)";//设置为被插入的图层名
            VariableDictionary.buttonText = "TJ(电气过建筑孔洞D)";
            VariableDictionary.layerColorIndex = 142;//设置为被插入的图层颜色
            VariableDictionary.textbox_Width = textBoxP_左右开洞.Text;
            VariableDictionary.textbox_Height = "15";
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        public void button_DQ_上下开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(电气过建筑孔洞D)";//设置为被插入的图层名
            VariableDictionary.buttonText = "TJ(电气过建筑孔洞D)";
            VariableDictionary.textbox_Width = "15";
            VariableDictionary.layerColorIndex = 142;//设置图层颜色
            VariableDictionary.textbox_Height = textBoxE_左右开洞.Text;
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        public void button_ZK_左右开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(自控过建筑)";
            VariableDictionary.textbox_Width = textBoxZ_左右开洞.Text;
            VariableDictionary.textbox_Height = "15";
            VariableDictionary.layerColorIndex = 3;//设置图层颜色
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        public void button_ZK_上下开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "TJ(自控过建筑)";
            VariableDictionary.textbox_Width = "15";
            VariableDictionary.layerColorIndex = 3;//设置图层颜色
            VariableDictionary.textbox_Height = textBoxZ_左右开洞.Text;
            Env.Document.SendStringToExecute("Rec2PolyLine_N ", false, false, false);
            //Env.Editor.Redraw();
        }

        #endregion


        private bool isMax = false;
        public void button_GY_工艺更多_Click(object sender, EventArgs e)
        {
            //237, 278
            //237, 170
            if (!isMax)
            {
                this.groupBox_工艺.Height = 300;
                //this.panel工艺.Height = 278;
                isMax = true;
            }
            else
            {
                this.groupBox_工艺.Height = 200;
                //this.panel工艺.Height = 170;
                isMax = false;
            }
        }
   
        public void button_清理_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("pu ", false, false, false);
        }
        public void button_Audit_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("audit\n y ", false, false, false);
        }
        public void button_DICTS_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("DICTS ", false, false, false);
        }
        public void button_CLEANUPDWG_Click(object sender, EventArgs e)
        {

            Env.Document.SendStringToExecute("CLEANUPDWG ", false, false, false);
        }
        public void textBox_Int_TextChanged(object sender, KeyPressEventArgs e)
        {
            // 检查输入的字符是否是数字或控制字符（如退格键）
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                // 如果不是数字，阻止输入并显示提示消息
                e.Handled = true;
                MessageBox.Show("只能输入数字，请重新输入！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public void textBox_Double_TextChanged(object sender, KeyPressEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // 允许数字、控制字符（如退格键）、小数点
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '.')
            {
                // 如果不是数字、控制字符或小数点，阻止输入并显示提示消息
                e.Handled = true;
                MessageBox.Show("只能输入数字和小数点，请重新输入！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // 确保小数点只能输入一次
            if (e.KeyChar == '.' && textBox.Text.Contains("."))
            {
                e.Handled = true; // 阻止输入
            }
        }
        public void textBox_inputKW_MouseDown(object sender, MouseEventArgs e)
        {
            if (textBox_inputKW.Text == "请输入功率")
                textBox_inputKW.Clear();
        }
        public void textBox_inputKW_MouseLeave(object sender, EventArgs e)
        {
            if (textBox_inputKW.Text.Length == 0)
                textBox_inputKW.Text = "请输入功率";
        }
        public void textBox_input10KW_MouseDown(object sender, MouseEventArgs e)
        {
            if (textBox_input10KW.Text == "请输入功率")
                textBox_input10KW.Clear();
            if (textBox_E_input10KW.Text == "请输入功率")
                textBox_E_input10KW.Clear();
        }
        public void textBox_input10KW_MouseLeave(object sender, EventArgs e)
        {
            if (textBox_input10KW.Text.Length == 0)
                textBox_input10KW.Text = "请输入功率";
            if (textBox_E_input10KW.Text.Length == 0)
                textBox_E_input10KW.Text = "请输入功率";
        }
        public void textBox_排水沟_深_MouseDown(object sender, MouseEventArgs e)
        {
            if (textBox_排水沟_深.Text == "请输入深")
                textBox_排水沟_深.Clear();
        }
        public void textBox_排水沟_深_MouseLeave(object sender, EventArgs e)
        {
            if (textBox_排水沟_深.Text.Length == 0)
                textBox_排水沟_深.Text = "请输入深";
        }
        public void textBox_排水沟_宽_MouseDown(object sender, MouseEventArgs e)
        {
            if (textBox_排水沟_宽.Text == "请输入宽")
                textBox_排水沟_宽.Clear();
        }
        public void textBox_排水沟_宽_MouseLeave(object sender, EventArgs e)
        {
            if (textBox_排水沟_宽.Text.Length == 0)
                textBox_排水沟_宽.Text = "请输入宽";
        }
        public void textBox_排风百分比_MouseDown(object sender, MouseEventArgs e)
        {
            if (textBox_排风百分比.Text == "排风百分比")
                textBox_排风百分比.Clear();
        }
        public void textBox_排风百分比_MouseLeave(object sender, EventArgs e)
        {
            if (textBox_排风百分比.Text.Length == 0)
                textBox_排风百分比.Text = "排风百分比";
        }
        public void textBox_荷载数据_MouseDown(object sender, MouseEventArgs e)
        {
            if (textBox_荷载数据.Text == "输入荷载数据")
                textBox_荷载数据.Clear();
        }
        public void textBox_荷载数据_MouseLeave(object sender, EventArgs e)
        {
            if (textBox_荷载数据.Text.Length == 0)
                textBox_荷载数据.Text = "输入荷载数据";
        }
        public void button_RECOVERY_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("DRAWINGRECOVERY ", false, false, false);
        }
        public void button_特殊TEXT_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnBlockLayer = "TJ(建筑专业J)";//设置为被插入的图层名
            VariableDictionary.layerColorIndex = 30;//设置为被插入的图层颜色
            VariableDictionary.btnFileName = "特殊地面做法要求";
            Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
        }
        public void button_checkNo_Click(object sender, EventArgs e)
        {
            if (VariableDictionary.btnState == false) { VariableDictionary.btnState = true; } else { VariableDictionary.btnState = false; }
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            //VariableDictionary.tjtBtn  = VariableDictionary.AtjtBtn;
            VariableDictionary.allTjtLayer = new List<string>
        {
            VariableDictionary.EtjtBtn[0],
            VariableDictionary.EtjtBtn[1],
            VariableDictionary.EtjtBtn[2],
            VariableDictionary.GtjtBtn[0],
           VariableDictionary. StjtBtn[0],
            VariableDictionary. PtjtBtn[0],
            VariableDictionary. NtjtBtn[0],
            VariableDictionary. ZKtjtBtn[0],
            VariableDictionary. ZKtjtBtn[1],
            VariableDictionary. ZKtjtBtn[2],
            "TJ(建筑专业J)"
        };
            VariableDictionary.allTjtLayer.Remove("TJ(建筑专业J)");
            VariableDictionary.allTjtLayer.Remove("TJ(房间编号)");
            VariableDictionary.allTjtLayer.Remove("TJ(建筑吊顶)");
            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);
        }
        /// <summary>
        /// 关闭共用图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_closeAllTJ_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            if (button_closeAllTJ.ForeColor.Name == "Black" || button_closeAllTJ.ForeColor.Name == "ControlText")
            {
                button_closeAllTJ.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                button_关闭工艺.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                button_关闭建筑.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                button_关闭结构.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                button_关闭给排水.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                button_关闭暖通.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                button_关闭电气.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                button_关闭自控.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_closeAllTJ.ForeColor = System.Drawing.SystemColors.ControlText;
                button_关闭工艺.ForeColor = System.Drawing.SystemColors.ControlText;
                button_关闭建筑.ForeColor = System.Drawing.SystemColors.ControlText;
                button_关闭结构.ForeColor = System.Drawing.SystemColors.ControlText;
                button_关闭给排水.ForeColor = System.Drawing.SystemColors.ControlText;
                button_关闭暖通.ForeColor = System.Drawing.SystemColors.ControlText;
                button_关闭电气.ForeColor = System.Drawing.SystemColors.ControlText;
                button_关闭自控.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.AtjtBtn)
            {
                VariableDictionary.allTjtLayer.Remove(item);
            }
            VariableDictionary.allTjtLayer.Add("TJ(建筑专业J)");
            VariableDictionary.allTjtLayer.Add("TJ(建筑吊顶)");
            VariableDictionary.allTjtLayer.Add("TJ(房间编号)");
            if (VariableDictionary.allTjtLayer.Contains("SB"))
                VariableDictionary.allTjtLayer.Remove("SB");
            if (VariableDictionary.allTjtLayer.Contains("1"))
                VariableDictionary.allTjtLayer.Remove("1");
            if (VariableDictionary.allTjtLayer.Contains("SB(设备名称)"))
                VariableDictionary.allTjtLayer.Remove("SB(设备名称)");
            if (VariableDictionary.allTjtLayer.Contains("QY"))
                VariableDictionary.allTjtLayer.Remove("QY");
            //VariableDictionary.allTjtLayer.Add("TJ()");
            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);


        }
        /// <summary>
        /// 生成外轮廓线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OutlineGenerator_Click(object sender, EventArgs e)
        {

            Env.Document.SendStringToExecute("SMARTOUTLINE ", false, false, false);
        }
        /// <summary>
        /// 只看共用图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_onlyAllTJ_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            if (button_closeAllTJ.ForeColor.Name == "Black" || button_closeAllTJ.ForeColor.Name == "ControlText")
            {
                button_closeAllTJ.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                VariableDictionary.btnState = true;
            }
            else
            {
                button_closeAllTJ.ForeColor = System.Drawing.SystemColors.ControlText;
                VariableDictionary.btnState = false;
            }
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("CloseLayer ", false, false, false);

        }
        public void button_test_Off_Btn_Click(object sender, EventArgs e)
        {
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.selectTjtLayer.Add("1");
            VariableDictionary.selectTjtLayer.Add("SB");
            Env.Document.SendStringToExecute("FindXrefLayersInViewport ", false, false, false);
        }
        public void button_test_On_Btn_Click(object sender, EventArgs e)
        {
            VariableDictionary.allTjtLayer.Clear();
            NewTjLayer();
            VariableDictionary.selectTjtLayer.Clear();
            VariableDictionary.selectTjtLayer.Add("1");
            VariableDictionary.selectTjtLayer.Add("SB");
            Env.Document.SendStringToExecute("FindXrefLayersInViewportOpen ", false, false, false);
        }

        #region 冻结图层
        /// <summary>
        /// 打开工艺视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OpenGYXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            if (button_OpenGYXref.ForeColor.Name == "Black" || button_OpenGYXref.ForeColor.Name == "ControlText")
            {
                //button_OpenGYXref.ForeColor = System.Drawing.SystemColors.ActiveCaption;
                button_OpenGYXref.Enabled = false;
                button_OffGYXref.Enabled = true;
                VariableDictionary.btnState = true;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewportOpen ", false, false, false);
        }
        /// <summary>
        /// 关闭工艺视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OffGYXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            if (button_OffGYXref.ForeColor.Name == "Black" || button_OffGYXref.ForeColor.Name == "ControlText")
            {
                button_OffGYXref.Enabled = false;
                button_OpenGYXref.Enabled = true;
                VariableDictionary.btnState = true;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewport ", false, false, false);
        }
        /// <summary>
        /// 打开建筑视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OpenJZXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OpenJZXref.Enabled = false;
            button_OffJZXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.AtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewportOpen ", false, false, false);
        }
        /// <summary>
        /// 关闭建筑视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OffJZXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OffJZXref.Enabled = false;
            button_OpenJZXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.AtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewport ", false, false, false);
        }
        /// <summary>
        /// 打开结构视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OpenJGXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OpenJGXref.Enabled = false;
            button_OffJGXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.StjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewportOpen ", false, false, false);
        }
        /// <summary>
        /// 关闭结构视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OffJGXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OffJGXref.Enabled = false;
            button_OpenJGXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.StjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewport ", false, false, false);
        }
        /// <summary>
        /// 打开暖通视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OpenNTXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OpenNTXref.Enabled = false;
            button_OffNTXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.NtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewportOpen ", false, false, false);
        }
        /// <summary>
        /// 关闭暖通视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OffNTXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OffNTXref.Enabled = false;
            button_OpenNTXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.NtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewport ", false, false, false);
        }
        /// <summary>
        /// 打开给排水视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OpenJPSXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OpenJPSXref.Enabled = false;
            button_OffJPSXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.PtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewportOpen ", false, false, false);
        }
        /// <summary>
        /// 关闭给排水视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OffJPSXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OffJPSXref.Enabled = false;
            button_OpenJPSXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.PtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewport ", false, false, false);
        }
        /// <summary>
        /// 打开电气视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OpenDQXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OpenDQXref.Enabled = false;
            button_OffDQXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.EtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewportOpen ", false, false, false);
        }
        /// <summary>
        /// 关闭电气视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OffDQXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OffDQXref.Enabled = false;
            button_OpenDQXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.EtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewport ", false, false, false);
        }
        /// <summary>
        /// 打开自控视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OpenZKXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OpenZKXref.Enabled = false;
            button_OffZKref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ZKtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewportOpen ", false, false, false);
        }
        /// <summary>
        /// 关闭自控视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OffZKref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OffZKref.Enabled = false;
            button_OpenZKXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ZKtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewport ", false, false, false);
        }
        /// <summary>
        /// 打开共用图层视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OpenGGXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OpenGGXref.Enabled = false;
            button_OffGGXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewportOpen ", false, false, false);
        }
        /// <summary>
        /// 关闭共用图层视口外参
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OffGGXref_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            button_OffGGXref.Enabled = false;
            button_OpenGGXref.Enabled = true;
            VariableDictionary.btnState = true;

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("FindXrefLayersInViewport ", false, false, false);
        }


        #endregion
        /// <summary>
        /// 插入表格
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btn_InputExcel_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("InsertExcelTableToCAD ", false, false, false);
        }
        /// <summary>
        /// 导出表格
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_outExcel_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("ExportCADTable ", false, false, false);
        }

        #region 工艺
        public void button工艺开闭工艺条件_Click(object sender, EventArgs e)
        {
            List<string> GGtjtBtn = new List<string>
        {
            "TJ(工艺专业GY)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button工艺开闭工艺条件.ForeColor.Name == "Black" || button工艺开闭工艺条件.ForeColor.Name == "ControlText")
            {
                button工艺开闭工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button工艺开闭工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button工艺开闭建筑条件_Click(object sender, EventArgs e)
        {
            List<string> GAtjtBtn = new List<string>
        {
            "TJ(建筑专业J)Y",
            "TJ(建筑专业J)N",
            "TJ(房间编号)",
            "QY",
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button工艺开闭建筑条件.ForeColor.Name == "Black" || button工艺开闭建筑条件.ForeColor.Name == "ControlText")
            {
                button工艺开闭建筑条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button工艺开闭建筑条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in GAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);

        }

        public void button工艺收工艺条件图层_Click(object sender, EventArgs e)
        {
            List<string> GGtjtBtn = new List<string>
        {
            "TJ(工艺专业GY)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button工艺收工艺条件图层.ForeColor.Name == "Black" || button工艺收工艺条件图层.ForeColor.Name == "ControlText")
            {
                button工艺收工艺条件图层.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button工艺收工艺条件图层.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button工艺收建筑过工艺条件_Click(object sender, EventArgs e)
        {
            List<string> GAtjtBtn = new List<string>
        {
            "TJ(建筑专业J)Y",
            "TJ(建筑专业J)N",
            "TJ(房间编号)",
            "QY",
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button工艺收建筑过工艺条件.ForeColor.Name == "Black" || button工艺收建筑过工艺条件.ForeColor.Name == "ControlText")
            {
                button工艺收建筑过工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button工艺收建筑过工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in GAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button工艺收建筑吊顶高度_Click(object sender, EventArgs e)
        {
            List<string> GAtjtBtn = new List<string>
        {
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button工艺收建筑吊顶高度.ForeColor.Name == "Black" || button工艺收建筑吊顶高度.ForeColor.Name == "ControlText")
            {
                button工艺收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button工艺收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in GAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button工艺收建筑房间编号_Click(object sender, EventArgs e)
        {
            List<string> GAtjtBtn = new List<string>
        {
            "TJ(房间编号)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button工艺收建筑房间编号.ForeColor.Name == "Black" || button工艺收建筑房间编号.ForeColor.Name == "ControlText")
            {
                button工艺收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button工艺收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in GAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }
        #endregion

        #region 电气
        public void button电气开关工艺条件_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气开关工艺条件.ForeColor.Name == "Black" || button电气开关工艺条件.ForeColor.Name == "ControlText")
            {
                button电气开关工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气开关工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收工艺条件_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
            {
                "TJ(电气专业D)",
                "TJ(电气专业D1)",
                "TJ(电气专业D2)",
                "SB(工艺设备)",
                "SB(设备名称)",
                "SB(设备外框)",
                "QY",
            };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;
            if (button电气收工艺条件.ForeColor.Name == "Black" || button电气收工艺条件.ForeColor.Name == "ControlText")
            {
                button电气收工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收工艺屋面放散管位置_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "TJ(电气专业D)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收工艺屋面放散管位置.ForeColor.Name == "Black" || button电气收工艺屋面放散管位置.ForeColor.Name == "ControlText")
            {
                button电气收工艺屋面放散管位置.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收工艺屋面放散管位置.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收工艺电气大于10kw_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "TJ(电气专业D2)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收工艺电气大于10kw.ForeColor.Name == "Black" || button电气收工艺电气大于10kw.ForeColor.Name == "ControlText")
            {
                button电气收工艺电气大于10kw.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收工艺电气大于10kw.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收工艺双电源条件_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "TJ(电气专业D1)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收工艺双电源条件.ForeColor.Name == "Black" || button电气收工艺双电源条件.ForeColor.Name == "ControlText")
            {
                button电气收工艺双电源条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收工艺双电源条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收工艺设备名称_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "SB(设备名称)",
            "SB(工艺设备)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收工艺设备名称.ForeColor.Name == "Black" || button电气收工艺设备名称.ForeColor.Name == "ControlText")
            {
                button电气收工艺设备名称.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收工艺设备名称.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }
        public void button洁净区域_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "QY"
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button洁净区域.ForeColor.Name == "Black" || button洁净区域.ForeColor.Name == "ControlText")
            {
                button洁净区域.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button洁净区域.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收工艺设备外框_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "SB(设备外框)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收工艺设备外框.ForeColor.Name == "Black" || button电气收工艺设备外框.ForeColor.Name == "ControlText")
            {
                button电气收工艺设备外框.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收工艺设备外框.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气开关建筑条件_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "TJ(房间编号)",
            "TJ(房间编号)",
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气开关建筑条件.ForeColor.Name == "Black" || button电气开关建筑条件.ForeColor.Name == "ControlText")
            {
                button电气开关建筑条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气开关建筑条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气开关给排水条件_Click(object sender, EventArgs e)
        {
            List<string> EPtjtBtn = new List<string>
        {
            "EQUIP_消火栓",
            "TJ(给排水过电气动力条件)",
            "TJ(给排水过电气喷淋条件)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气开关给排水条件.ForeColor.Name == "Black" || button电气开关给排水条件.ForeColor.Name == "ControlText")
            {
                button电气开关给排水条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气开关给排水条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EPtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气开关暖通条件_Click(object sender, EventArgs e)
        {

            List<string> ENtjtBtn = new List<string>
        {
           "TJ(暖通过电气)",
           "暖通过电气其他条件"
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气开关暖通条件.ForeColor.Name == "Black" || button电气开关暖通条件.ForeColor.Name == "ControlText")
            {
                button电气开关暖通条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气开关暖通条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ENtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收建筑底图_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收建筑底图.ForeColor.Name == "Black" || button电气收建筑底图.ForeColor.Name == "ControlText")
            {
                button电气收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收建筑电动卷帘门_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "电动卷帘门",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收建筑电动卷帘门.ForeColor.Name == "Black" || button电气收建筑电动卷帘门.ForeColor.Name == "ControlText")
            {
                button电气收建筑电动卷帘门.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收建筑电动卷帘门.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收建筑防火卷帘门_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "防火卷帘门",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收建筑防火卷帘门.ForeColor.Name == "Black" || button电气收建筑防火卷帘门.ForeColor.Name == "ControlText")
            {
                button电气收建筑防火卷帘门.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收建筑防火卷帘门.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收建筑电动排烟窗_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "电动排烟窗",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收建筑电动排烟窗.ForeColor.Name == "Black" || button电气收建筑电动排烟窗.ForeColor.Name == "ControlText")
            {
                button电气收建筑电动排烟窗.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收建筑电动排烟窗.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收建筑房间编号_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "TJ(房间编号)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收建筑房间编号.ForeColor.Name == "Black" || button电气收建筑房间编号.ForeColor.Name == "ControlText")
            {
                button电气收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收建筑吊顶高度_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收建筑吊顶高度.ForeColor.Name == "Black" || button电气收建筑吊顶高度.ForeColor.Name == "ControlText")
            {
                button电气收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收给排水消火栓_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "EQUIP_消火栓",
            "EQUIP-消火栓",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收给排水消火栓.ForeColor.Name == "Black" || button电气收给排水消火栓.ForeColor.Name == "ControlText")
            {
                button电气收给排水消火栓.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收给排水消火栓.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收给排水动力条件_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "TJ(给排水过电气动力条件)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收给排水动力条件.ForeColor.Name == "Black" || button电气收给排水动力条件.ForeColor.Name == "ControlText")
            {
                button电气收给排水动力条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收给排水动力条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收给排水喷淋有关条件_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "TJ(给排水过电气喷淋条件)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收给排水喷淋有关条件.ForeColor.Name == "Black" || button电气收给排水喷淋有关条件.ForeColor.Name == "ControlText")
            {
                button电气收给排水喷淋有关条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收给排水喷淋有关条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收暖通文字条件_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "TJ(暖通过电气)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收暖通文字条件.ForeColor.Name == "Black" || button电气收暖通文字条件.ForeColor.Name == "ControlText")
            {
                button电气收暖通文字条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收暖通文字条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button电气收暖通其他条件_Click(object sender, EventArgs e)
        {
            List<string> EAtjtBtn = new List<string>
        {
            "暖通其他条件",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button电气收暖通其他条件.ForeColor.Name == "Black" || button电气收暖通其他条件.ForeColor.Name == "ControlText")
            {
                button电气收暖通其他条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button电气收暖通其他条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in EAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        #endregion

        #region 给排水
        public void button给排水开闭工艺条件_Click(object sender, EventArgs e)
        {
            //    List<string> PGtjtBtn = new List<string>
            //{
            //    "TJ(给排水专业S)",
            //    "SB(工艺设备)",
            //    "SB(设备名称)",
            //    "SB(设备外框)",
            //    "QY",
            //};
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水开闭工艺条件.ForeColor.Name == "Black" || button给排水开闭工艺条件.ForeColor.Name == "ControlText")
            {
                button给排水开闭工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水开闭工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            //foreach (var item in PGtjtBtn)
            //{
            //    VariableDictionary.selectTjtLayer.Remove(item);
            //}
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button给排水收工艺条件_Click(object sender, EventArgs e)
        {
            List<string> PGtjtBtn = new List<string>
        {
            "TJ(给排水专业S)",
            "EQUIP_地漏",
            "EQUIP_给水",
            "SB(工艺设备)",
            "SB(设备名称)",
            "SB(设备外框)",
            "QY",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收工艺条件.ForeColor.Name == "Black" || button给排水收工艺条件.ForeColor.Name == "ControlText")
            {
                button给排水收工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水收工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in PGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button给排水收工艺设备_Click(object sender, EventArgs e)
        {
            List<string> PGtjtBtn = new List<string>
        {
            "SB(工艺设备)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收工艺设备.ForeColor.Name == "Black" || button给排水收工艺设备.ForeColor.Name == "ControlText")
            {
                button给排水收工艺设备.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水收工艺设备.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in PGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button给排水收工艺洁净区域划分_Click(object sender, EventArgs e)
        {
            List<string> PGtjtBtn = new List<string>
        {
            "QY",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收工艺洁净区域划分.ForeColor.Name == "Black" || button给排水收工艺洁净区域划分.ForeColor.Name == "ControlText")
            {
                button给排水收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in PGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button给排水收工艺设备名称_Click(object sender, EventArgs e)
        {
            List<string> PGtjtBtn = new List<string>
        {
            "SB(设备名称)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收工艺设备名称.ForeColor.Name == "Black" || button给排水收工艺设备名称.ForeColor.Name == "ControlText")
            {
                button给排水收工艺设备名称.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水收工艺设备名称.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in PGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);

        }

        public void button给排水收工艺设备外框_Click(object sender, EventArgs e)
        {
            List<string> PGtjtBtn = new List<string>
        {
            "SB(设备外框)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收工艺设备外框.ForeColor.Name == "Black" || button给排水收工艺设备外框.ForeColor.Name == "ControlText")
            {
                button给排水收工艺设备外框.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水收工艺设备外框.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in PGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);

        }

        public void button给排水收建筑底图_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收建筑底图.ForeColor.Name == "Black" || button给排水收建筑底图.ForeColor.Name == "ControlText")
            {
                button给排水收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button给排水收建筑房间编号_Click(object sender, EventArgs e)
        {
            List<string> PAtjtBtn = new List<string>
        {
             "TJ(房间编号)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收建筑房间编号.ForeColor.Name == "Black" || button给排水收建筑房间编号.ForeColor.Name == "ControlText")
            {
                button给排水收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in PAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button给排水收建筑吊顶高度_Click(object sender, EventArgs e)
        {
            List<string> PAtjtBtn = new List<string>
        {
             "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收建筑吊顶高度.ForeColor.Name == "Black" || button给排水收建筑吊顶高度.ForeColor.Name == "ControlText")
            {
                button给排水收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in PAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button给排水开闭建筑条件_Click(object sender, EventArgs e)
        {
            List<string> PAtjtBtn = new List<string>
        {
             "TJ(房间编号)",
             "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水开闭建筑条件.ForeColor.Name == "Black" || button给排水开闭建筑条件.ForeColor.Name == "ControlText")
            {
                button给排水开闭建筑条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button给排水收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button给排水收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button给排水收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水开闭建筑条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button给排水收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button给排水收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button给排水收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in PAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }
        public void button给排水收暖通过条件_Click(object sender, EventArgs e)
        {
            List<string> PNtjtBtn = new List<string>
        {
             "TJ(暖通过给排水)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收暖通过条件.ForeColor.Name == "Black" || button给排水收暖通过条件.ForeColor.Name == "ControlText")
            {
                button给排水收暖通过条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水收暖通过条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in PNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }
        public void button给排水收暖通文字条件_Click(object sender, EventArgs e)
        {
            List<string> PNtjtBtn = new List<string>
        {
             "TJ(暖通过给排水)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水收暖通文字条件.ForeColor.Name == "Black" || button给排水收暖通文字条件.ForeColor.Name == "ControlText")
            {
                button给排水收暖通文字条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;

            }
            else
            {
                button给排水收暖通文字条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in PNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }
        public void button给排水开闭暖通条件_Click(object sender, EventArgs e)
        {
            List<string> ENtjtBtn = new List<string>
        {
           "TJ(暖通过给排水)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button给排水开闭暖通条件.ForeColor.Name == "Black" || button给排水开闭暖通条件.ForeColor.Name == "ControlText")
            {
                button给排水开闭暖通条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button给排水收暖通过条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button给排水收暖通文字条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button给排水开闭暖通条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button给排水收暖通过条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button给排水收暖通文字条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ENtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }
        #endregion

        #region 自控

        public void button自控开关工艺条件_Click(object sender, EventArgs e)
        {
            //    List<string> ZGtjtBtn = new List<string>
            //{
            //    "EQUIP-通讯",
            //    "EQUIP-安防",
            //    "SB(工艺设备)",
            //    "SB(设备名称)",
            //    "SB(设备外框)",
            //    "QY",
            //};
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控开关工艺条件.ForeColor.Name == "Black" || button自控开关工艺条件.ForeColor.Name == "ControlText")
            {
                button自控开关工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控开关工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ZKtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }



        public void button自控开关给排水条件_Click(object sender, EventArgs e)
        {
            List<string> ZPtjtBtn = new List<string>
                {
                    "EQUIP-通讯",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控开关给排水条件.ForeColor.Name == "Black" || button自控开关给排水条件.ForeColor.Name == "ControlText")
            {
                button自控开关给排水条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控开关给排水条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZPtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控开关暖通条件_Click(object sender, EventArgs e)
        {
            List<string> ZNtjtBtn = new List<string>
                {
                    "TJ(暖通过自控)",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控开关暖通条件.ForeColor.Name == "Black" || button自控开关暖通条件.ForeColor.Name == "ControlText")
            {
                button自控开关暖通条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控开关暖通条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }



        public void button自控收工艺通讯条件_Click(object sender, EventArgs e)
        {
            List<string> ZGtjtBtn = new List<string>
        {
            "EQUIP-安防",
            "EQUIP-通讯",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收工艺通讯条件.ForeColor.Name == "Black" || button自控收工艺通讯条件.ForeColor.Name == "ControlText")
            {
                button自控收工艺通讯条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button自控收工艺安防条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收工艺通讯条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button自控收工艺安防条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in ZGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收工艺安防条件_Click(object sender, EventArgs e)
        {
            List<string> ZGtjtBtn = new List<string>
        {
            "EQUIP-安防",
            "EQUIP-通讯",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收工艺安防条件.ForeColor.Name == "Black" || button自控收工艺安防条件.ForeColor.Name == "ControlText")
            {
                button自控收工艺安防条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button自控收工艺通讯条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收工艺安防条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button自控收工艺通讯条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in ZGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收工艺设备名称_Click(object sender, EventArgs e)
        {
            List<string> ZGtjtBtn = new List<string>
        {
            "SB(设备名称)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收工艺设备名称.ForeColor.Name == "Black" || button自控收工艺设备名称.ForeColor.Name == "ControlText")
            {
                button自控收工艺设备名称.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收工艺设备名称.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收工艺设备外框_Click(object sender, EventArgs e)
        {
            List<string> ZGtjtBtn = new List<string>
        {
            "SB(设备外框)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收工艺设备外框.ForeColor.Name == "Black" || button自控收工艺设备外框.ForeColor.Name == "ControlText")
            {
                button自控收工艺设备外框.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收工艺设备外框.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收工艺洁净区域划分_Click(object sender, EventArgs e)
        {
            List<string> ZGtjtBtn = new List<string>
        {
            "QY",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收工艺洁净区域划分.ForeColor.Name == "Black" || button自控收工艺洁净区域划分.ForeColor.Name == "ControlText")
            {
                button自控收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控开关建筑条件_Click(object sender, EventArgs e)
        {
            List<string> ZGtjtBtn = new List<string>
        {
            "TJ(房间编号)",
             "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控开关建筑条件.ForeColor.Name == "Black" || button自控开关建筑条件.ForeColor.Name == "ControlText")
            {
                button自控开关建筑条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控开关建筑条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in ZGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收建筑底图_Click(object sender, EventArgs e)
        {

            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收建筑底图.ForeColor.Name == "Black" || button自控收建筑底图.ForeColor.Name == "ControlText")
            {
                button自控收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收建筑房间编号_Click(object sender, EventArgs e)
        {
            List<string> ZGtjtBtn = new List<string>
        {
            "TJ(房间编号)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收建筑房间编号.ForeColor.Name == "Black" || button自控收建筑房间编号.ForeColor.Name == "ControlText")
            {
                button自控收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收建筑吊顶高度_Click(object sender, EventArgs e)
        {
            List<string> ZGtjtBtn = new List<string>
        {
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收建筑吊顶高度.ForeColor.Name == "Black" || button自控收建筑吊顶高度.ForeColor.Name == "ControlText")
            {
                button自控收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收给排水通讯条件_Click(object sender, EventArgs e)
        {
            List<string> ZPtjtBtn = new List<string>
                {
                    "EQUIP-通讯",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收给排水通讯条件.ForeColor.Name == "Black" || button自控收给排水通讯条件.ForeColor.Name == "ControlText")
            {
                button自控收给排水通讯条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收给排水通讯条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZPtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收暖通文字条件_Click(object sender, EventArgs e)
        {
            List<string> ZPtjtBtn = new List<string>
                {
                    "TJ(暖通过自控)",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收暖通文字条件.ForeColor.Name == "Black" || button自控收暖通文字条件.ForeColor.Name == "ControlText")
            {
                button自控收暖通文字条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收暖通文字条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZPtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收暖通高中效过滤排风_Click(object sender, EventArgs e)
        {
            List<string> ZNtjtBtn = new List<string>
                {
                    "暖通专业原有图层",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收暖通高中效过滤排风.ForeColor.Name == "Black" || button自控收暖通高中效过滤排风.ForeColor.Name == "ControlText")
            {
                button自控收暖通高中效过滤排风.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收暖通高中效过滤排风.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收暖通风阀_Click(object sender, EventArgs e)
        {
            List<string> ZNtjtBtn = new List<string>
                {
                    "暖通专业原有图层",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收暖通风阀.ForeColor.Name == "Black" || button自控收暖通风阀.ForeColor.Name == "ControlText")
            {
                button自控收暖通风阀.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收暖通风阀.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收暖通FFU_Click(object sender, EventArgs e)
        {
            List<string> ZNtjtBtn = new List<string>
                {
                    "暖通专业原有图层",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收暖通FFU.ForeColor.Name == "Black" || button自控收暖通FFU.ForeColor.Name == "ControlText")
            {
                button自控收暖通FFU.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收暖通FFU.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收暖通VAV阀_Click(object sender, EventArgs e)
        {
            List<string> ZNtjtBtn = new List<string>
                {
                    "暖通专业原有图层",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收暖通VAV阀.ForeColor.Name == "Black" || button自控收暖通VAV阀.ForeColor.Name == "ControlText")
            {
                button自控收暖通VAV阀.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收暖通VAV阀.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收暖通空调机组_Click(object sender, EventArgs e)
        {
            List<string> ZNtjtBtn = new List<string>
                {
                    "暖通专业原有图层",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收暖通空调机组.ForeColor.Name == "Black" || button自控收暖通空调机组.ForeColor.Name == "ControlText")
            {
                button自控收暖通空调机组.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收暖通空调机组.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收暖通流程图_Click(object sender, EventArgs e)
        {
            List<string> ZNtjtBtn = new List<string>
                {
                    "暖通专业原有图层",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收暖通流程图.ForeColor.Name == "Black" || button自控收暖通流程图.ForeColor.Name == "ControlText")
            {
                button自控收暖通流程图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收暖通流程图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收暖通压差梯度_Click(object sender, EventArgs e)
        {
            List<string> ZNtjtBtn = new List<string>
                {
                    "暖通专业原有图层",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收暖通压差梯度.ForeColor.Name == "Black" || button自控收暖通压差梯度.ForeColor.Name == "ControlText")
            {
                button自控收暖通压差梯度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收暖通压差梯度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }
        public void button自控开关电气条件_Click(object sender, EventArgs e)
        {
            List<string> ZNtjtBtn = new List<string>
                {
                    "TEL_CABINET",
                    "EQUIP-照明",
                    "WIRE-厂区消防",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控开关电气条件.ForeColor.Name == "Black" || button自控开关电气条件.ForeColor.Name == "ControlText")
            {
                button自控开关电气条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button自控收电气机房配电柜.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button自控收电气路灯.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button自控收电气厂区消防线.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控开关电气条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button自控收电气机房配电柜.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button自控收电气路灯.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button自控收电气厂区消防线.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收电气机房配电柜_Click(object sender, EventArgs e)
        {
            List<string> ZEtjtBtn = new List<string>
                {
                    "TEL_CABINET",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收电气机房配电柜.ForeColor.Name == "Black" || button自控收电气机房配电柜.ForeColor.Name == "ControlText")
            {
                button自控收电气机房配电柜.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收电气机房配电柜.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收电气路灯_Click(object sender, EventArgs e)
        {
            List<string> ZEtjtBtn = new List<string>
                {
                    "EQUIP-照明",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收电气路灯.ForeColor.Name == "Black" || button自控收电气路灯.ForeColor.Name == "ControlText")
            {
                button自控收电气路灯.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收电气路灯.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button自控收电气厂区消防线_Click(object sender, EventArgs e)
        {
            List<string> ZEtjtBtn = new List<string>
                {
                    "WIRE-厂区消防",
                };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button自控收电气厂区消防线.ForeColor.Name == "Black" || button自控收电气厂区消防线.ForeColor.Name == "ControlText")
            {
                button自控收电气厂区消防线.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button自控收电气厂区消防线.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ZEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        #endregion

        #region 结构
        public void button结构开关给排水条件_Click(object sender, EventArgs e)
        {
            List<string> SPtjtBtn = new List<string>
        {
            "TJ(给排水过结构)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构开关给排水条件.ForeColor.Name == "Black" || button结构开关给排水条件.ForeColor.Name == "ControlText")
            {
                button结构开关给排水条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收给排水设备基础.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收给排水套管.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关给排水条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收给排水设备基础.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收给排水套管.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SPtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收给排水设备基础_Click(object sender, EventArgs e)
        {

            List<string> SPtjtBtn = new List<string>
        {
            "TJ(给排水过结构)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收给排水设备基础.ForeColor.Name == "Black" || button结构收给排水设备基础.ForeColor.Name == "ControlText")
            {
                button结构开关给排水条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收给排水设备基础.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收给排水套管.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关给排水条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收给排水设备基础.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收给排水套管.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SPtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收给排水套管_Click(object sender, EventArgs e)
        {

            List<string> SPtjtBtn = new List<string>
        {
            "TJ(给排水过结构)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收给排水套管.ForeColor.Name == "Black" || button结构收给排水套管.ForeColor.Name == "ControlText")
            {
                button结构开关给排水条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收给排水设备基础.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收给排水套管.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关给排水条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收给排水设备基础.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收给排水套管.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SPtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构开关暖通条件_Click(object sender, EventArgs e)
        {
            List<string> SNtjtBtn = new List<string>
        {
            "TJ(暖通过结构)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构开关暖通条件.ForeColor.Name == "Black" || button结构开关暖通条件.ForeColor.Name == "ControlText")
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收暖通楼板洞口_Click(object sender, EventArgs e)
        {
            List<string> SNtjtBtn = new List<string>
        {
            "TJ(暖通过结构)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收暖通楼板洞口.ForeColor.Name == "Black" || button结构收暖通楼板洞口.ForeColor.Name == "ControlText")
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收暖通地沟_Click(object sender, EventArgs e)
        {
            List<string> SNtjtBtn = new List<string>
        {
            "TJ(暖通过结构)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收暖通地沟.ForeColor.Name == "Black" || button结构收暖通地沟.ForeColor.Name == "ControlText")
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收暖通设备基础_Click(object sender, EventArgs e)
        {
            List<string> SNtjtBtn = new List<string>
        {
            "TJ(暖通过结构)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收暖通设备基础.ForeColor.Name == "Black" || button结构收暖通设备基础.ForeColor.Name == "ControlText")
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收暖通吊挂风机及荷载条件_Click(object sender, EventArgs e)
        {
            List<string> SNtjtBtn = new List<string>
        {
            "TJ(暖通过结构)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收暖通吊挂风机及荷载条件.ForeColor.Name == "Black" || button结构收暖通吊挂风机及荷载条件.ForeColor.Name == "ControlText")
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关暖通条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通楼板洞口.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通地沟.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通设备基础.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收暖通吊挂风机及荷载条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SNtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构开关电气条件_Click(object sender, EventArgs e)
        {
            List<string> SEtjtBtn = new List<string>
        {
            "TJ(电气过结构)",
            "TJ(电气过结构楼板洞D)",
            "TJ(电气过结构电缆沟D)",
            "TJ(电气过结构活荷载D)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构开关电气条件.ForeColor.Name == "Black" || button结构开关电气条件.ForeColor.Name == "ControlText")
            {
                button结构开关电气条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收电气楼板洞口.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收电气电缆沟.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收电气荷载条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关电气条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收电气楼板洞口.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收电气电缆沟.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收电气荷载条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收电气楼板洞口_Click(object sender, EventArgs e)
        {
            List<string> SEtjtBtn = new List<string>
        {
            "TJ(电气过结构楼板洞D)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收电气楼板洞口.ForeColor.Name == "Black" || button结构收电气楼板洞口.ForeColor.Name == "ControlText")
            {
                button结构收电气楼板洞口.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构收电气楼板洞口.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收电气电缆沟_Click(object sender, EventArgs e)
        {
            List<string> SEtjtBtn = new List<string>
        {
            "TJ(电气过结构电缆沟D)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收电气电缆沟.ForeColor.Name == "Black" || button结构收电气电缆沟.ForeColor.Name == "ControlText")
            {
                button结构收电气电缆沟.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构收电气电缆沟.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收电气荷载条件_Click(object sender, EventArgs e)
        {
            List<string> SEtjtBtn = new List<string>
        {

            "TJ(电气过结构活荷载D)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收电气荷载条件.ForeColor.Name == "Black" || button结构收电气荷载条件.ForeColor.Name == "ControlText")
            {
                button结构收电气荷载条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构收电气荷载条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构开关自控条件_Click(object sender, EventArgs e)
        {
            List<string> SZtjtBtn = new List<string>
        {
            "TJ(自控过结构)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构开关自控条件.ForeColor.Name == "Black" || button结构开关自控条件.ForeColor.Name == "ControlText")
            {
                button结构开关自控条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收自控楼板洞口.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关自控条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收自控楼板洞口.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SZtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收自控楼板洞口_Click(object sender, EventArgs e)
        {
            List<string> SZtjtBtn = new List<string>
        {
            "TJ(自控过结构)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收自控楼板洞口.ForeColor.Name == "Black" || button结构收自控楼板洞口.ForeColor.Name == "ControlText")
            {
                button结构开关自控条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收自控楼板洞口.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关自控条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收自控楼板洞口.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SZtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收工艺设备名称_Click(object sender, EventArgs e)
        {
            List<string> SGtjtBtn = new List<string>
        {
            "SB(设备名称)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收工艺设备名称.ForeColor.Name == "Black" || button结构收工艺设备名称.ForeColor.Name == "ControlText")
            {
                button结构收工艺设备名称.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构收工艺设备名称.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收工艺设备外框_Click(object sender, EventArgs e)
        {
            List<string> SGtjtBtn = new List<string>
        {
            "SB(设备外框)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收工艺设备外框.ForeColor.Name == "Black" || button结构收工艺设备外框.ForeColor.Name == "ControlText")
            {
                button结构收工艺设备外框.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构收工艺设备外框.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收工艺设备_Click(object sender, EventArgs e)
        {
            List<string> SGtjtBtn = new List<string>
        {
            "SB(工艺设备)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收工艺设备.ForeColor.Name == "Black" || button结构收工艺设备.ForeColor.Name == "ControlText")
            {
                button结构收工艺设备.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构收工艺设备.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收工艺过结构条件_Click(object sender, EventArgs e)
        {
            List<string> SGtjtBtn = new List<string>
        {
            "TJ(结构专业JG)",
            "SB(工艺设备)",
            "SB(设备名称)",
            "SB(设备外框)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收工艺过结构条件.ForeColor.Name == "Black" || button结构收工艺过结构条件.ForeColor.Name == "ControlText")
            {
                button结构收工艺过结构条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构收工艺过结构条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in SGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Remove(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构开关工艺条件_Click(object sender, EventArgs e)
        {
            //    List<string> SGtjtBtn = new List<string>
            //{
            //    "TJ(结构专业JG)",
            //    "SB(工艺设备)",
            //    "SB(设备名称)",
            //    "SB(设备外框)",
            //};
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构开关工艺条件.ForeColor.Name == "Black" || button结构开关工艺条件.ForeColor.Name == "ControlText")
            {
                button结构开关工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收工艺过结构条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收工艺设备.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收工艺设备外框.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收工艺设备名称.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构开关工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收工艺过结构条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收工艺设备.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收工艺设备外框.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收工艺设备名称.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收建筑底图_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收建筑底图.ForeColor.Name == "Black" || button结构收建筑底图.ForeColor.Name == "ControlText")
            {
                button结构收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button结构收建筑房间编号_Click(object sender, EventArgs e)
        {
            List<string> SAtjtBtn = new List<string>
        {
            "TJ(房间编号)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button结构收建筑房间编号.ForeColor.Name == "Black" || button结构收建筑房间编号.ForeColor.Name == "ControlText")
            {
                button结构收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button结构收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in SAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button开闭建筑条件_Click(object sender, EventArgs e)
        {
            //    List<string> SAtjtBtn = new List<string>
            //{
            //    "TJ(房间编号)",
            //};
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button开闭建筑条件.ForeColor.Name == "Black" || button开闭建筑条件.ForeColor.Name == "ControlText")
            {
                button开闭建筑条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button结构收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button开闭建筑条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button结构收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            //foreach (var item in SAtjtBtn)
            //{
            //    VariableDictionary.selectTjtLayer.Add(item);
            //}
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        #endregion

        #region 暖通
        public void button_暖通开关工艺条件_Click(object sender, EventArgs e)
        {
            //    List<string> NGtjtBtn = new List<string>
            //{
            //    "TJ(暖通专业N)",
            //    "SB(工艺设备)",
            //    "SB(设备名称)",
            //    "SB(设备外框)",
            //    "QY",
            //};
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button_暖通开关工艺条件.ForeColor.Name == "Black" || button_暖通开关工艺条件.ForeColor.Name == "ControlText")
            {
                button_暖通开关工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button暖通收工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button暖通收工艺设备.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button暖通收工艺设备名称.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button暖通收工艺设备外框.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button暖通收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button_暖通开关工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button暖通收工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button暖通收工艺设备.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button暖通收工艺设备名称.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button暖通收工艺设备外框.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button暖通收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button暖通收工艺条件_Click(object sender, EventArgs e)
        {
            List<string> NGtjtBtn = new List<string>
        {
            "TJ(暖通专业N)",
            "SB(工艺设备)",
            "SB(设备名称)",
            "SB(设备外框)",
            "QY",
            "SB",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button暖通收工艺条件.ForeColor.Name == "Black" || button暖通收工艺条件.ForeColor.Name == "ControlText")
            {
                button暖通收工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button暖通收工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in NGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Remove(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button暖通收工艺设备_Click(object sender, EventArgs e)
        {
            List<string> NGtjtBtn = new List<string>
        {
            "SB(工艺设备)",
            "SB",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button暖通收工艺设备.ForeColor.Name == "Black" || button暖通收工艺设备.ForeColor.Name == "ControlText")
            {
                button暖通收工艺设备.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button暖通收工艺设备.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in NGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button暖通收工艺设备名称_Click(object sender, EventArgs e)
        {
            List<string> NGtjtBtn = new List<string>
        {
            "SB(设备名称)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button暖通收工艺设备名称.ForeColor.Name == "Black" || button暖通收工艺设备名称.ForeColor.Name == "ControlText")
            {
                button暖通收工艺设备名称.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button暖通收工艺设备名称.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in NGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button暖通收工艺设备外框_Click(object sender, EventArgs e)
        {
            List<string> NGtjtBtn = new List<string>
        {
            "SB(设备外框)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button暖通收工艺设备外框.ForeColor.Name == "Black" || button暖通收工艺设备外框.ForeColor.Name == "ControlText")
            {
                button暖通收工艺设备外框.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button暖通收工艺设备外框.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in NGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button暖通收工艺洁净区域划分_Click(object sender, EventArgs e)
        {
            List<string> NGtjtBtn = new List<string>
        {
            "QY",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button暖通收工艺洁净区域划分.ForeColor.Name == "Black" || button暖通收工艺洁净区域划分.ForeColor.Name == "ControlText")
            {
                button暖通收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button暖通收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in NGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button暖通开关建筑条件_Click(object sender, EventArgs e)
        {
            //    List<string> NAtjtBtn = new List<string>
            //{
            //    "TJ(房间编号)",
            //    "TJ(建筑专业J)Y",
            //    "TJ(建筑吊顶)",
            //};
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button暖通开关建筑条件.ForeColor.Name == "Black" || button暖通开关建筑条件.ForeColor.Name == "ControlText")
            {
                button暖通开关建筑条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button暖通收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button暖通收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button暖通收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button暖通开关建筑条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button暖通收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button暖通收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button暖通收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            //foreach (var item in NAtjtBtn)
            //{
            //    VariableDictionary.selectTjtLayer.Add(item);
            //}
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button暖通收建筑底图_Click(object sender, EventArgs e)
        {
            List<string> NAtjtBtn = new List<string>
            {
                "TJ(房间编号)",
                "TJ(建筑专业J)Y",
                "TJ(建筑吊顶)",
            };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button暖通收建筑底图.ForeColor.Name == "Black" || button暖通收建筑底图.ForeColor.Name == "ControlText")
            {
                button暖通收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button暖通收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();

            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button暖通收建筑房间编号_Click(object sender, EventArgs e)
        {
            List<string> NAtjtBtn = new List<string>
        {
            "TJ(房间编号)",

        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button暖通收建筑房间编号.ForeColor.Name == "Black" || button暖通收建筑房间编号.ForeColor.Name == "ControlText")
            {
                button暖通收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button暖通收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in NAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button暖通收建筑吊顶高度_Click(object sender, EventArgs e)
        {
            List<string> NAtjtBtn = new List<string>
        {
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button暖通收建筑吊顶高度.ForeColor.Name == "Black" || button暖通收建筑吊顶高度.ForeColor.Name == "ControlText")
            {
                button暖通收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button暖通收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in NAtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        #endregion

        #region 建筑

        public void button建筑开闭工艺条件_Click(object sender, EventArgs e)
        {
            //List<string> AGtjtBtn = new List<string>();
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑开闭工艺条件.ForeColor.Name == "Black" || button建筑开闭工艺条件.ForeColor.Name == "ControlText")
            {
                button建筑开闭工艺条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑开闭工艺条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);

        }

        public void button建筑收工艺房间编号_Click(object sender, EventArgs e)
        {
            List<string> AGtjtBtn = new List<string>
        {
            "TJ(房间编号)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收工艺房间编号.ForeColor.Name == "Black" || button建筑收工艺房间编号.ForeColor.Name == "ControlText")
            {
                button建筑收工艺房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收工艺房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }
            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收工艺吊顶高度_Click(object sender, EventArgs e)
        {
            List<string> AGtjtBtn = new List<string>
        {
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收工艺吊顶高度.ForeColor.Name == "Black" || button建筑收工艺吊顶高度.ForeColor.Name == "ControlText")
            {
                button建筑收工艺吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收工艺吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收工艺洁净区域划分_Click(object sender, EventArgs e)
        {
            List<string> AGtjtBtn = new List<string>
        {
            "QY",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收工艺洁净区域划分.ForeColor.Name == "Black" || button建筑收工艺洁净区域划分.ForeColor.Name == "ControlText")
            {
                button建筑收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收工艺洁净区域划分.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收工艺过建筑条件_Click(object sender, EventArgs e)
        {
            List<string> AGtjtBtn = new List<string>
        {
            "TJ(建筑专业J)",
            "TJ(建筑专业J)Y",
            "TJ(建筑专业J)N",
            "TJ(建筑吊顶)",
            "QY",
            "TJ(房间编号)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收工艺过建筑条件.ForeColor.Name == "Black" || button建筑收工艺过建筑条件.ForeColor.Name == "ControlText")
            {
                button建筑收工艺过建筑条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收工艺过建筑条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in VariableDictionary.GtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in AGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Remove(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }


        public void button建筑收建筑底图_Click(object sender, EventArgs e)
        {
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收建筑底图.ForeColor.Name == "Black" || button建筑收建筑底图.ForeColor.Name == "ControlText")
            {
                button建筑收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();

            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收建筑房间编号_Click(object sender, EventArgs e)
        {
            List<string> AGtjtBtn = new List<string>
        {
            "TJ(房间编号)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收建筑房间编号.ForeColor.Name == "Black" || button建筑收建筑房间编号.ForeColor.Name == "ControlText")
            {
                button建筑收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收建筑吊顶高度_Click(object sender, EventArgs e)
        {
            List<string> AGtjtBtn = new List<string>
        {
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收建筑吊顶高度.ForeColor.Name == "Black" || button建筑收建筑吊顶高度.ForeColor.Name == "ControlText")
            {
                button建筑收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑开闭建筑条件_Click(object sender, EventArgs e)
        {
            List<string> AGtjtBtn = new List<string>
        {
            "TJ(建筑专业J)",
            "TJ(建筑专业J)Y",
            "TJ(建筑专业J)N",
            "TJ(房间编号)",
            "TJ(建筑吊顶)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑开闭建筑条件.ForeColor.Name == "Black" || button建筑开闭建筑条件.ForeColor.Name == "ControlText")
            {
                button建筑开闭建筑条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收建筑房间编号.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收建筑底图.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑开闭建筑条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收建筑吊顶高度.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收建筑房间编号.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收建筑底图.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AGtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            foreach (var item in VariableDictionary.ApmtTjBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }
            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }


        public void button建筑收结构柱_Click(object sender, EventArgs e)
        {
            List<string> AStjtBtn = new List<string>
        {
            "COLU",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收结构柱.ForeColor.Name == "Black" || button建筑收结构柱.ForeColor.Name == "ControlText")
            {
                button建筑收结构柱.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收结构柱.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AStjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收结构楼板洞_Click(object sender, EventArgs e)
        {
            List<string> AStjtBtn = new List<string>
        {
            "HOLE",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收结构楼板洞.ForeColor.Name == "Black" || button建筑收结构楼板洞.ForeColor.Name == "ControlText")
            {
                button建筑收结构楼板洞.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收结构楼板洞.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AStjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收结构混凝土墙_Click(object sender, EventArgs e)
        {
            List<string> AStjtBtn = new List<string>
        {
            "WALL-C",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收结构混凝土墙.ForeColor.Name == "Black" || button建筑收结构混凝土墙.ForeColor.Name == "ControlText")
            {
                button建筑收结构混凝土墙.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收结构混凝土墙.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AStjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑开闭结构条件_Click(object sender, EventArgs e)
        {
            List<string> AStjtBtn = new List<string>
        {
            "WALL-C",
            "HOLE",
            "COLU",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑开闭结构条件.ForeColor.Name == "Black" || button建筑开闭结构条件.ForeColor.Name == "ControlText")
            {
                button建筑收结构柱.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收结构楼板洞.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收结构混凝土墙.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑开闭结构条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收结构柱.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收结构楼板洞.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收结构混凝土墙.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑开闭结构条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AStjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收给排水孔洞_Click(object sender, EventArgs e)
        {
            List<string> APtjtBtn = new List<string>
        {
            "TJ(给排水过建筑)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收给排水孔洞.ForeColor.Name == "Black" || button建筑收给排水孔洞.ForeColor.Name == "ControlText")
            {
                button建筑收给排水孔洞.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收给排水孔洞.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in APtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收给排水消火栓_Click(object sender, EventArgs e)
        {
            List<string> APtjtBtn = new List<string>
        {
            "EQUIP_消火栓",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收给排水消火栓.ForeColor.Name == "Black" || button建筑收给排水消火栓.ForeColor.Name == "ControlText")
            {
                button建筑收给排水消火栓.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收给排水消火栓.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in APtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑开闭给排水条件_Click(object sender, EventArgs e)
        {
            List<string> APtjtBtn = new List<string>
        {
            "TJ(给排水过建筑)",
            "EQUIP_消火栓",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑开闭给排水条件.ForeColor.Name == "Black" || button建筑开闭给排水条件.ForeColor.Name == "ControlText")
            {
                button建筑开闭给排水条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收给排水消火栓.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收给排水孔洞.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑开闭给排水条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收给排水消火栓.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收给排水孔洞.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in APtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收暖通孔洞_Click(object sender, EventArgs e)
        {
            List<string> ANtjtBtn = new List<string>
        {
            "TJ(暖通过建筑)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收暖通孔洞.ForeColor.Name == "Black" || button建筑收暖通孔洞.ForeColor.Name == "ControlText")
            {
                button建筑收暖通孔洞.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收暖通孔洞.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ANtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收暖通自然排烟窗_Click(object sender, EventArgs e)
        {
            List<string> ANtjtBtn = new List<string>
        {
            "TJ(暖通过建筑)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收暖通自然排烟窗.ForeColor.Name == "Black" || button建筑收暖通自然排烟窗.ForeColor.Name == "ControlText")
            {
                button建筑收暖通自然排烟窗.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收暖通自然排烟窗.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ANtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收暖通夹墙_Click(object sender, EventArgs e)
        {
            List<string> ANtjtBtn = new List<string>
        {
            "WALL-PARAPET",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收暖通夹墙.ForeColor.Name == "Black" || button建筑收暖通夹墙.ForeColor.Name == "ControlText")
            {
                button建筑收暖通夹墙.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收暖通夹墙.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ANtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收暖通排水沟_Click(object sender, EventArgs e)
        {
            List<string> ANtjtBtn = new List<string>
        {
            "TJ(暖通过建筑)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收暖通排水沟.ForeColor.Name == "Black" || button建筑收暖通排水沟.ForeColor.Name == "ControlText")
            {
                button建筑收暖通排水沟.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收暖通排水沟.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ANtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑开闭暖通条件_Click(object sender, EventArgs e)
        {
            List<string> ANtjtBtn = new List<string>
        {
            "TJ(暖通过建筑)",
            "WALL-PARAPET",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑开闭暖通条件.ForeColor.Name == "Black" || button建筑开闭暖通条件.ForeColor.Name == "ControlText")
            {
                button建筑开闭暖通条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收暖通排水沟.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收暖通夹墙.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收暖通自然排烟窗.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收暖通孔洞.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑开闭暖通条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收暖通排水沟.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收暖通夹墙.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收暖通自然排烟窗.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收暖通孔洞.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in ANtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收电气条件_Click(object sender, EventArgs e)
        {
            List<string> AEtjtBtn = new List<string>
        {
            "TJ(电气过建筑)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收电气条件.ForeColor.Name == "Black" || button建筑收电气条件.ForeColor.Name == "ControlText")
            {
                button建筑收电气条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收电气条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收电气夹墙_Click(object sender, EventArgs e)
        {
            List<string> AEtjtBtn = new List<string>
        {

            "TJ(电气过建筑夹墙D)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收电气夹墙.ForeColor.Name == "Black" || button建筑收电气夹墙.ForeColor.Name == "ControlText")
            {
                button建筑收电气夹墙.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收电气夹墙.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收电气孔洞_Click(object sender, EventArgs e)
        {
            List<string> AEtjtBtn = new List<string>
        {
            "TJ(电气过建筑孔洞D)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收电气孔洞.ForeColor.Name == "Black" || button建筑收电气孔洞.ForeColor.Name == "ControlText")
            {
                button建筑收电气孔洞.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收电气孔洞.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑开闭电气条件_Click(object sender, EventArgs e)
        {
            List<string> AEtjtBtn = new List<string>
        {
            "TJ(电气过建筑孔洞D)",
            "TJ(电气过建筑夹墙D)",
            "TJ(电气过建筑)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑开闭电气条件.ForeColor.Name == "Black" || button建筑开闭电气条件.ForeColor.Name == "ControlText")
            {
                button建筑收电气条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑开闭电气条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收电气夹墙.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收电气孔洞.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收电气条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑开闭电气条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收电气夹墙.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收电气孔洞.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AEtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑收自控条件_Click(object sender, EventArgs e)
        {
            List<string> AZtjtBtn = new List<string>
        {
            "TJ(自控过建筑)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑收自控条件.ForeColor.Name == "Black" || button建筑收自控条件.ForeColor.Name == "ControlText")
            {
                button建筑收自控条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑收自控条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AZtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }

        public void button建筑开闭自控条件_Click(object sender, EventArgs e)
        {
            List<string> AZtjtBtn = new List<string>
        {
            "TJ(自控过建筑)",
        };
            VariableDictionary.tjtBtn = VariableDictionary.tjtBtnNull;

            if (button建筑开闭自控条件.ForeColor.Name == "Black" || button建筑开闭自控条件.ForeColor.Name == "ControlText")
            {
                button建筑开闭自控条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
                button建筑收自控条件.ForeColor = System.Drawing.SystemColors.ActiveCaption; VariableDictionary.btnState = true;
            }
            else
            {
                button建筑开闭自控条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
                button建筑收自控条件.ForeColor = System.Drawing.SystemColors.ControlText; VariableDictionary.btnState = false;
            }

            NewTjLayer();//初始化allTjLayer
            VariableDictionary.selectTjtLayer.Clear();
            foreach (var item in AZtjtBtn)
            {
                VariableDictionary.selectTjtLayer.Add(item);
            }

            Env.Document.SendStringToExecute("IsFrozenLayer ", false, false, false);
        }


        #endregion

        public void button_分解块_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("ExplodeBlockToNewBlock ", false, false, false);
        }

        public void button_测量房间面积_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnBlockLayer = "暖通房间面积";//设置为被插入的图层名
            VariableDictionary.buttonText = "暖通房间面积";
            VariableDictionary.layerColorIndex = 6;//设置图层颜色

            Env.Document.SendStringToExecute("AreaByPoints ", false, false, false);
        }

        public void button_给排水过结构矩形开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.textbox_Height = textBox_给排水过结构矩形开洞Y.Text;
            VariableDictionary.textbox_Width = textBox_给排水过结构矩形开洞X.Text;
            VariableDictionary.btnBlockLayer = "TJ(给排水过结构)";
            VariableDictionary.buttonText = "PTJ_矩形开洞";
            VariableDictionary.layerColorIndex = 7;
            VariableDictionary.btnFileName = "TJ(给排水过结构)";
            VariableDictionary.textbox_RecPlus_Text = textBox_给排水过结构矩形外扩.Text;
            if (Convert.ToDouble(VariableDictionary.textbox_Height) > 0 && Convert.ToDouble(VariableDictionary.textbox_Width) > 0)
            {
                recAndMRec = 0;
                Env.Document.SendStringToExecute("DrawRec ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("Rec2PolyLine ", false, false, false);
            }
        }

        public void button_给排水过结构直径开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(给排水过结构)";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.buttonText = "PTJ_圆形开洞";
            VariableDictionary.btnBlockLayer = "TJ(给排水过结构)";
            VariableDictionary.textBox_S_CirDiameter = Convert.ToDouble(textBox_给排水过结构直径开洞直径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_给排水过结构直径外扩.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 7;
            if (VariableDictionary.textBox_S_CirDiameter == 0)
            {
                Env.Document.SendStringToExecute("CirDiameter ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirDiameter_2 ", false, false, false);
            }
        }

        public void button_给排水过结构半径开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(给排水过结构)";
            VariableDictionary.buttonText = "PTJ_圆形开洞";
            VariableDictionary.btnBlockLayer = "TJ(给排水过结构)";
            VariableDictionary.textbox_S_Cirradius = Convert.ToDouble(textBox_给排水过结构半径开洞半径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_给排水过结构半径外扩.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 7;
            if (VariableDictionary.textbox_S_Cirradius == 0)
            {
                Env.Document.SendStringToExecute("CirRadius ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirRadius_2 ", false, false, false);
            }
        }

        public void button_自控过结构矩形开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.textbox_Height = textBox_自控过结构矩形开洞Y.Text;
            VariableDictionary.textbox_Width = textBox_自控过结构矩形开洞X.Text;
            VariableDictionary.btnBlockLayer = "TJ(自控过结构)";
            VariableDictionary.buttonText = "ZK_矩形开洞";
            VariableDictionary.layerColorIndex = 3;
            VariableDictionary.btnFileName = "TJ(自控过结构)";
            VariableDictionary.textbox_RecPlus_Text = textBox_自控过结构矩形开洞外扩.Text;
            if (Convert.ToDouble(VariableDictionary.textbox_Height) > 0 && Convert.ToDouble(VariableDictionary.textbox_Width) > 0)
            {
                recAndMRec = 0;
                Env.Document.SendStringToExecute("DrawRec ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("Rec2PolyLine ", false, false, false);
            }
        }

        public void button_自控过结构直径开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(自控过结构)";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.buttonText = "ZK_圆形开洞";
            VariableDictionary.btnBlockLayer = "TJ(自控过结构)";
            VariableDictionary.textBox_S_CirDiameter = Convert.ToDouble(textBox_自控过结构直径开洞直径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_自控过结构直径开洞外扩.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 3;
            if (VariableDictionary.textBox_S_CirDiameter == 0)
            {
                Env.Document.SendStringToExecute("CirDiameter ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirDiameter_2 ", false, false, false);
            }
        }

        public void button_自控过结构半径开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(自控过结构)";
            VariableDictionary.buttonText = "ZK_圆形开洞";
            VariableDictionary.btnBlockLayer = "TJ(自控过结构)";
            VariableDictionary.textbox_S_Cirradius = Convert.ToDouble(textBox_自控过结构半径开洞半径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_自控过结构半径开洞外扩.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 3;
            if (VariableDictionary.textbox_S_Cirradius == 0)
            {
                Env.Document.SendStringToExecute("CirRadius ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirRadius_2 ", false, false, false);
            }
        }

        public void button_暖通_矩形开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.textbox_Height = textBox_暖通_矩形Y.Text;
            VariableDictionary.textbox_Width = textBox_暖通_矩形X.Text;
            VariableDictionary.btnBlockLayer = "TJ(暖通过结构)";
            VariableDictionary.buttonText = "NTJ_矩形开洞";
            VariableDictionary.layerColorIndex = 6;
            VariableDictionary.btnFileName = "TJ(暖通过结构)";
            VariableDictionary.textbox_RecPlus_Text = textBox_矩形外扩值.Text;
            if (Convert.ToDouble(VariableDictionary.textbox_Height) > 0 && Convert.ToDouble(VariableDictionary.textbox_Width) > 0)
            {
                recAndMRec = 0;
                Env.Document.SendStringToExecute("DrawRec ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("Rec2PolyLine ", false, false, false);
            }

        }

        public void button_暖通_直径开圆洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(暖通过结构)";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.buttonText = "NTJ_圆形开洞";
            VariableDictionary.btnBlockLayer = "TJ(暖通过结构)";
            VariableDictionary.textBox_S_CirDiameter = Convert.ToDouble(textBox_暖通_直径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_暖通_直径外扩值.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 6;
            if (VariableDictionary.textBox_S_CirDiameter == 0)
            {
                Env.Document.SendStringToExecute("CirDiameter ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirDiameter_2 ", false, false, false);
            }
            ;
        }

        public void button_暖通_半径开圆洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(暖通过结构)";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.buttonText = "NTJ_圆形开洞";
            VariableDictionary.btnBlockLayer = "TJ(暖通过结构)";
            VariableDictionary.textbox_S_Cirradius = Convert.ToDouble(textBox_暖通_半径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_暖通_半径外扩值.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 6;
            if (VariableDictionary.textbox_S_Cirradius == 0)
            {
                Env.Document.SendStringToExecute("CirRadius ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirRadius_2 ", false, false, false);
            }
        }

        public void button_电气过结构矩形开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.textbox_Height = textBox_电气过结构矩形Y.Text;
            VariableDictionary.textbox_Width = textBox_电气过结构矩形X.Text;
            VariableDictionary.btnBlockLayer = "TJ(电气过结构楼板洞D)";
            VariableDictionary.buttonText = "ETJ_矩形开洞";
            VariableDictionary.layerColorIndex = 142;
            VariableDictionary.btnFileName = "TJ(电气过结构楼板洞D)";
            VariableDictionary.textbox_RecPlus_Text = textBox_电气过结构矩形外扩.Text;
            if (Convert.ToDouble(VariableDictionary.textbox_Height) > 0 && Convert.ToDouble(VariableDictionary.textbox_Width) > 0)
            {
                recAndMRec = 0;
                Env.Document.SendStringToExecute("DrawRec ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("Rec2PolyLine ", false, false, false);
            }
        }

        public void button_电气过结构半径开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(电气过结构楼板洞D)";
            VariableDictionary.buttonText = "ETJ_圆形开洞";
            VariableDictionary.btnBlockLayer = "TJ(电气过结构楼板洞D)";
            VariableDictionary.textbox_S_Cirradius = Convert.ToDouble(textBox_电气过结构半径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_电气过结构半径外扩.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 142;
            if (VariableDictionary.textbox_S_Cirradius == 0)
            {
                Env.Document.SendStringToExecute("CirRadius ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirRadius_2 ", false, false, false);
            }
        }

        public void button_电气过结构直径开洞_Click(object sender, EventArgs e)
        {
            VariableDictionary.btnFileName = "TJ(电气过结构楼板洞D)";
            VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.buttonText = "ETJ_圆形开洞";
            VariableDictionary.btnBlockLayer = "TJ(电气过结构楼板洞D)";
            VariableDictionary.textBox_S_CirDiameter = Convert.ToDouble(textBox_电气过结构直径.Text);//拿到指定圆的直径
            VariableDictionary.textbox_CirPlus_Text = textBox_电气过结构直径外扩.Text;//拿到指定圆的外扩量
            VariableDictionary.layerColorIndex = 142;
            if (VariableDictionary.textBox_S_CirDiameter == 0)
            {
                Env.Document.SendStringToExecute("CirDiameter ", false, false, false);
            }
            else
            {
                Env.Document.SendStringToExecute("CirDiameter_2 ", false, false, false);
            }
        }
        /// <summary>
        /// 生成属性块
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_DrawElement_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("CreateAttributeBlock ", false, false, false);
        }
        /// <summary>
        /// 同步数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_SyncExcel_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("GenerateEquipmentTable ", false, false, false);
        }
        /// <summary>
        /// 编辑选中图元
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_SelcetElement_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("EditAttributeBlock ", false, false, false);
        }

        /// <summary>
        /// 生成设备表格
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_ShowTable_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("GenerateEquipmentTable ", false, false, false);
        }
        /// <summary>
        /// 导出Excel表格
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button_OutExcelTable_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("ExportTableToExcel ", false, false, false);
        }


        public void button_进口管道_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "GB_PID_JKGD_进口管道";
            VariableDictionary.btnFileName_blockName = "#GB_PID_JKGD_进口管道";
            VariableDictionary.btnBlockLayer = "管道";
            VariableDictionary.resourcesFile = Resources.GB_PID_GDJK_进口管道;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }

        public void button_出口管道_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "GB_PID_CKGD_出口管道";
            VariableDictionary.btnFileName_blockName = "#GB_PID_CKGD_出口管道";
            VariableDictionary.btnBlockLayer = "管道";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.GB_PID_GDJK_出口管道;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }

        public void button_蝶阀_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "GB_PID_FMDF_蝶阀";
            VariableDictionary.btnFileName_blockName = "#GB_PID_FMDF_蝶阀";
            VariableDictionary.btnBlockLayer = "蝶阀";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.GB_PID_FMDF_蝶阀;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }

        public void button_法兰_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "GB_PID_FL_法兰";
            VariableDictionary.btnFileName_blockName = "#GB_PID_FL_法兰";
            VariableDictionary.btnBlockLayer = "法兰";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.GB_PID_FL_法兰;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }

        public void button_异径管_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "GB_PID_YJJG_异径接管";
            VariableDictionary.btnFileName_blockName = "#GB_PID_YJJG_异径接管";
            VariableDictionary.btnBlockLayer = "异径接管";
            //VariableDictionary.btnBlockLayer = VariableDictionary.btnFileName;
            VariableDictionary.resourcesFile = Resources.GB_PID_YJJG_异径接管;
            Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
        }

        public void button_属性同步_Click(object sender, EventArgs e)
        {
            Env.Document.SendStringToExecute("SyncAttribute ", false, false, false);
        }

        public void button_绘制进口管道_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "GB_PID_JKGD_进口管道";
            VariableDictionary.btnFileName_blockName = "#GB_PID_JKGD_进口管道";
            VariableDictionary.btnBlockLayer = "管道";
            VariableDictionary.resourcesFile = Resources.GB_PID_GDJK_进口管道;
            Env.Document.SendStringToExecute("Draw_GD_PipeLine_DynamicBlock ", false, false, false);
        }

        public void button_绘制出口管道_Click(object sender, EventArgs e)
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "GB_PID_CKGD_出口管道";
            VariableDictionary.btnFileName_blockName = "#GB_PID_CKGD_出口管道";
            VariableDictionary.btnBlockLayer = "管道";
            VariableDictionary.resourcesFile = Resources.GB_PID_GDJK_出口管道;
            Env.Document.SendStringToExecute("Draw_GD_PipeLine_DynamicBlock ", false, false, false);
        }

        public void button_绘图_Click(object sender, EventArgs e)
        {
            // 初始状态：隐藏TabPage（可选）
            //if (isTabPageVisible)
            //{
            //    tabCtl_Main.TabPages.Remove(linkedTabPage);
            //    isTabPageVisible = false; 
            //}
            //else
            //{
            //    tabCtl_Main.TabPages.Add(linkedTabPage);
            //    isTabPageVisible = true;
            //}
        }

        public void button120_Click(object sender, EventArgs e)
        {

        }
    }
}
