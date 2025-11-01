using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 统一按钮命令管理器
    /// </summary>
    public static class UnifiedButtonCommandManager
    {
        /// <summary>
        /// 按钮名称与对应方法的映射
        /// </summary>
        private static readonly Dictionary<string, Action> _buttonCommandMap = new Dictionary<string, Action>
    {
        // 工艺按钮映射
        { "纯化水", () => ExecuteProcessCommand("纯化水", "PW,DN??,??L/h", "TJ(工艺专业GY)", 40, 0) },
        { "纯蒸汽", () => ExecuteProcessCommand("纯蒸汽", "LS,DN??,??MPa,??kg/h,", "TJ(工艺专业GY)", 40, 0) },
        { "注射用水", () => ExecuteProcessCommand("注射用水", "WFI,DN??,??℃,??L/h,使用量??L/h", "TJ(工艺专业GY)", 40, 0) },
        { "氧气", () => ExecuteProcessCommand("氧气", "O2,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0) },
        { "氮气", () => ExecuteProcessCommand("氮气", "N2,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0) },
        { "二氧化碳", () => ExecuteProcessCommand("二氧化碳", "CO2,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0) },
        { "无菌压缩空气", () => ExecuteProcessCommand("无菌压缩空气", "CA,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0) },
        { "仪表压缩空气", () => ExecuteProcessCommand("仪表压缩空气", "IA,DN??,??MPa,??L/min", "TJ(工艺专业GY)", 40, 0) },
        { "低压蒸汽", () => ExecuteProcessCommand("低压蒸汽", "LS,DN??,??MPa,??kg/h,", "TJ(工艺专业GY)", 40, 0) },
        { "低温循环上水", () => ExecuteProcessCommand("低温循环上水", "RWS,DN??,??m³/h", "TJ(工艺专业GY)", 40, 0) },
        { "常温循环上水", () => ExecuteProcessCommand("常温循环上水", "CWS,DN??,??m³/h", "TJ(工艺专业GY)", 40, 0) },
        { "凝结回水", () => ExecuteProcessCommand("凝结回水", "SC,DN??", "TJ(工艺专业GY)", 40, 0) },
        
        // 公共图按钮映射
        { "共用条件", () => ExecuteCommonCondition() },
        { "所有条件开关", () => ExecuteAllConditionSwitch() },
        { "设备开关", () => ExecuteEquipmentSwitch() },
        
        // 设备表相关按钮
        { "设备表导入", () => ExecuteEquipmentImport() },
        { "设备表导出", () => ExecuteEquipmentExport() },
        { "区域开关", () => ExecuteAreaSwitch() },

          // 方向按键映射
        { "上", () => ExecuteDirectionCommand("上", Math.PI * 1.5) },      // 逆时针90度
        { "右上", () => ExecuteDirectionCommand("右上", Math.PI * 1.25) },  // 逆时针135度
        { "右", () => ExecuteDirectionCommand("右", Math.PI * 1) },        // 180度
        { "右下", () => ExecuteDirectionCommand("右下", Math.PI * 0.75) },  // 逆时针135度
        { "下", () => ExecuteDirectionCommand("下", Math.PI * 0.5) },      // 逆时针90度
        { "左下", () => ExecuteDirectionCommand("左下", Math.PI * 0.25) },  // 逆时针45度
        { "左", () => ExecuteDirectionCommand("左", 0) },                 // 原位置
        { "左上", () => ExecuteDirectionCommand("左上", Math.PI * 1.75) },   // 顺时针45度

          #region 建筑专业按钮映射
        { "吊顶", () => ExecuteBuildingCommand("吊顶", "JZTJ_吊顶", "TJ(建筑吊顶)", 30) },
        { "不吊顶", () => ExecuteBuildingCommand("不吊顶", "JZTJ_不吊顶", "TJ(建筑吊顶)", 30) },
        { "防撞护板", () => ExecuteBuildingCommand("防撞护板", "JZTJ_防撞护板", "TJ(建筑专业J)", 30) },
        { "房间编号", () => ExecuteRoomNumberCommand() },
        { "编号检查", () => ExecuteBuildingCommand("编号检查", "JZTJ_编号检查", "TJ(建筑专业J)", 30) },
        { "冷藏库降板", () => ExecuteBuildingCommand("冷藏库降板", "冷藏库降板（270）", "TJ(建筑专业J)", 30) },
        { "冷冻库降板", () => ExecuteBuildingCommand("冷冻库降板", "冷冻库降板（390）", "TJ(建筑专业J)", 30) },
        { "特殊地面做法要求", () => ExecuteBuildingCommand("特殊地面做法要求", "JZTJ_特殊地面做法要求", "TJ(建筑专业J)", 30) },
        { "排水沟", () => ExecuteDrainageCommand() },
        { "横墙建筑开洞", () => ExecuteBuildingWallHoleCommand("横墙") },
        { "纵墙建筑开洞", () => ExecuteBuildingWallHoleCommand("纵墙") },
        #endregion

        #region 结构专业按钮映射
        { "结构受力点", () => ExecuteStructureLoadPointCommand() },
        { "水平荷载", () => ExecuteHorizontalLoadCommand() },
        { "面着地", () => ExecuteSurfaceGroundCommand() },
        { "框着地", () => ExecuteFrameGroundCommand() },
        { "圆形开洞", () => ExecuteCircularHoleCommand() },
        { "半径开圆洞", () => ExecuteRadiusHoleCommand() },
        { "矩形开洞", () => ExecuteRectangularHoleCommand() },
        #endregion

        #region 给排水专业按钮映射
        { "洗眼器", () => ExecutePlumbingCommand("洗眼器", "PTJ_洗眼器", "$TWTSYS$00000604", "TJ(给排水专业S)", 142, Resources.PTJ_洗眼器) },
        { "不给饮用水", () => ExecutePlumbingCommand("不给饮用水", "不给饮用水", "", "TJ(给排水专业S)", 7, null) },
        { "小便器给水", () => ExecutePlumbingCommand("小便器给水", "PTJ_小便器给水", "$TWTSYS$00000603", "TJ(给排水专业S)", 0, Resources.PTJ_小便器给水) },
        { "大便器给水", () => ExecutePlumbingCommand("大便器给水", "PTJ_大便器给水", "$TWTSYS$00000602", "TJ(给排水专业S)", 0, Resources.PTJ_大便器给水) },
        { "洗涤盆", () => ExecutePlumbingCommand("洗涤盆", "PTJ_洗涤盆", "普通区洗涤盆", "TJ(给排水专业S)", 0, Resources.PTJ_洗涤盆) },
        { "水池给水", () => ExecutePlumbingCommand("水池给水", "PTJ_水池给水", "$TWTSYS$00000605", "TJ(给排水专业S)", 0, Resources.PTJ_水池给水) },
        #endregion

        #region 暖通专业按钮映射
        { "排潮", () => ExecuteHVACCommand("排潮", "(排潮)", "TJ(暖通专业N)", 6) },
        { "排尘", () => ExecuteHVACCommand("排尘", "(排尘)", "TJ(暖通专业N)", 6) },
        { "排热", () => ExecuteHVACCommand("排热", "(排热)", "TJ(暖通专业N)", 6) },
        { "直排", () => ExecuteHVACCommand("直排", "(直排)", "TJ(暖通专业N)", 6) },
        { "除味", () => ExecuteHVACCommand("除味", "(除味)", "TJ(暖通专业N)", 6) },
        { "A级高度", () => ExecuteHVACCommand("A级高度", "(A级高度？米)", "TJ(暖通专业N)", 6) },
        { "设备取风量", () => ExecuteHVACCommand("设备取风量", "(设备取风量 ？m³/h)", "TJ(暖通专业N)", 6) },
        { "设备排风量", () => ExecuteHVACCommand("设备排风量", "(设备排风量 ？m³/h)", "TJ(暖通专业N)", 6) },
        { "排风百分比", () => ExecuteHVACPercentageCommand() },
        { "温度", () => ExecuteHVACCommand("温度", "(温度 ？℃±？℃)", "TJ(暖通专业N)", 6) },
        { "湿度", () => ExecuteHVACCommand("湿度", "(湿度 ？%±？%)", "TJ(暖通专业N)", 6) },
        #endregion

        #region 电气专业按钮映射
        { "无线AP", () => ExecuteElectricalCommand("无线AP", "ZKTJ_EQUIP_无线AP", "$equip$00001857", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_无线AP, 500) },
        { "电话插座", () => ExecuteElectricalCommand("电话插座", "ZKTJ_EQUIP_电话插座", "$equip$00001867", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_电话插座, 500) },
        { "网络插座", () => ExecuteElectricalCommand("网络插座", "ZKTJ_EQUIP_网络插座", "$equip$00001847", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_网络插座, 500) },
        { "电话网络插座", () => ExecuteElectricalCommand("电话网络插座", "ZKTJ_EQUIP_电话网络插座", "ZKTJ-电话网络插座", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_电话网络插座, 500) },
        { "安防监控", () => ExecuteElectricalCommand("安防监控", "ZKTJ_EQUIP_安防监控", "HC002695005706", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_安防监控, 500) },
        { "眼纹识别器", () => ExecuteElectricalCommand("眼纹识别器", "ZKTJ_EQUIP_眼纹识别器", "$equip$00002616", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_眼纹识别器, 0) },
        { "无线网络接入点", () => ExecuteElectricalCommand("无线网络接入点", "ZKTJ_EQUIP_无线AP", "$equip$00003217", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_无线AP, 0) },
        { "室外彩色云台摄像机", () => ExecuteElectricalCommand("室外彩色云台摄像机", "ZKTJ_EQUIP_室外彩色云台摄像机", "$equip$00002970", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_室外彩色云台摄像机, 0) },
        #endregion

        #region 自控专业按钮映射
        { "外线电话插座", () => ExecuteControlCommand("外线电话插座", "ZKTJ_EQUIP_外线电话插座", "$Equip$00003196", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_外线电话插座, 0) },
        { "网络交换机", () => ExecuteControlCommand("网络交换机", "ZKTJ_EQUIP_网络交换机", "$equip$00002332", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_网络交换机, 0) },
        { "室外彩色摄像机", () => ExecuteControlCommand("室外彩色摄像机", "ZKTJ_EQUIP_室外彩色摄像机", "$equip$00002969", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_室外彩色摄像机, 0) },
        { "人像识别器", () => ExecuteControlCommand("人像识别器", "ZKTJ_EQUIP_人像识别器", "$equip$00002496", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_人像识别器, 0) },
        { "内线电话插座", () => ExecuteControlCommand("内线电话插座", "ZKTJ_EQUIP_内线电话插座", "$Equip$00003195", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_内线电话插座, 0) },
        { "门磁开关", () => ExecuteControlCommand("门磁开关", "ZKTJ_EQUIP_门磁开关", "$equip$00002621", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_门磁开关, 0) },
        { "局域网插座", () => ExecuteControlCommand("局域网插座", "ZKTJ_EQUIP_局域网插座", "$Equip$00003198", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_局域网插座, 0) },
        { "门禁控制器", () => ExecuteControlCommand("门禁控制器", "ZKTJ_EQUIP_门禁控制器", "$equip_U$00000028", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_门禁控制器, 0) },
        { "读卡器", () => ExecuteControlCommand("读卡器", "ZKTJ_EQUIP_读卡器", "$equip$00002617", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_读卡器, 0) },
        { "带扬声器电话机", () => ExecuteControlCommand("带扬声器电话机", "ZKTJ_EQUIP_带扬声器电话机", "$equip$00003042", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_带扬声器电话机, 0) },
        { "互联网插座", () => ExecuteControlCommand("互联网插座", "ZKTJ_EQUIP_互联网插座", "$Equip$00003197", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_互联网插座, 0) },
        { "广角彩色摄像机", () => ExecuteControlCommand("广角彩色摄像机", "ZKTJ_EQUIP_广角彩色摄像机", "$equip$00002731", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_广角彩色摄像机, 0) },
        { "防爆型网络摄像机", () => ExecuteControlCommand("防爆型网络摄像机", "ZKTJ_EQUIP_防爆型网络摄像机", "$equip$00002975", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_防爆型网络摄像机, 0) },
        { "防爆型电话机", () => ExecuteControlCommand("防爆型电话机", "ZKTJ_EQUIP_防爆型电话机", "$equip$00003047", "EQUIP-通讯", 3, Resources.ZKTJ_EQUIP_防爆型电话机, 0) },
        { "半球彩色摄像机", () => ExecuteControlCommand("半球彩色摄像机", "ZKTJ_EQUIP_半球彩色摄像机", "$equip$00002353", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_半球彩色摄像机, 0) },
        { "电锁按键", () => ExecuteControlCommand("电锁按键", "ZKTJ_EQUIP_电锁按键", "$equip$00002375", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_电锁按键, 500) },
        { "电控锁", () => ExecuteControlCommand("电控锁", "ZKTJ_EQUIP_电控锁", "$equip$00002474", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_电控锁, 500) },
        { "监控文字", () => ExecuteControlCommand("监控文字", "ZKTJ_EQUIP_监控文字", "ZKTJ-EQUIP-监控", "EQUIP-安防", 3, Resources.ZKTJ_EQUIP_监控文字, 0) },
        #endregion
    };


        /// <summary>
        /// 执行方向命令的通用方法
        /// </summary>
        private static void ExecuteDirectionCommand(string direction, double rotateAngle)
        {
            try
            {
                VariableDictionary.entityRotateAngle = rotateAngle;

                // 根据按钮名称设置不同的旋转角度（针对特殊图元）
                if (VariableDictionary.btnFileName.Contains("摄像机"))
                {
                    switch (direction)
                    {
                        case "上":
                            VariableDictionary.entityRotateAngle = Math.PI * 1.5; // 逆时针90度
                            break;
                        case "右上":
                            VariableDictionary.entityRotateAngle = Math.PI * 1.25; // 顺时针135度
                            break;
                        case "右":
                            VariableDictionary.entityRotateAngle = Math.PI * 1; // 180度
                            break;
                        case "右下":
                            VariableDictionary.entityRotateAngle = Math.PI * 0.75; // 逆时针135度
                            break;
                        case "下":
                            VariableDictionary.entityRotateAngle = Math.PI * 0.5; // 逆时针90度
                            break;
                        case "左下":
                            VariableDictionary.entityRotateAngle = Math.PI * 0.25; // 逆时针45度
                            break;
                        case "左":
                            VariableDictionary.entityRotateAngle = 0; // 原位置
                            break;
                        case "左上":
                            VariableDictionary.entityRotateAngle = Math.PI * 1.75; // 顺时针45度
                            break;
                    }
                }
                else
                {
                    // 其他图元使用传入的角度
                    VariableDictionary.entityRotateAngle = rotateAngle;
                }

                // 根据图元类型选择不同的插入命令
                if (VariableDictionary.btnFileName.Contains("电话插座") || VariableDictionary.btnFileName.Contains("网络插座"))
                {
                    Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
                }
                else
                {
                    Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行方向命令时出错: {ex.Message}");
            }
        }
        /// <summary>
        /// 执行所有条件开关
        /// </summary>
        private static void ExecuteAllConditionSwitch()
        {
            // 实现所有条件开关的逻辑
            // 这里可以调用FormMain中的相应方法
        }
        /// <summary>
        /// 执行共用条件
        /// </summary>
        private static void ExecuteCommonCondition()
        {
            VariableDictionary.entityRotateAngle = 0;
            VariableDictionary.btnFileName = "共用条件说明";
            VariableDictionary.btnBlockLayer = "TJ(共用条件)";
            VariableDictionary.layerColorIndex = 161;
            Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
        }

        #region 工艺命令
        /// <summary>
        /// 执行工艺命令的通用方法
        /// </summary>
        private static void ExecuteProcessCommand(string commandName, string btnFileName, string btnBlockLayer, int layerColorIndex, double rotateAngle)
        {
            try
            {
                VariableDictionary.entityRotateAngle = rotateAngle;
                VariableDictionary.btnFileName = btnFileName;
                VariableDictionary.btnBlockLayer = btnBlockLayer;
                VariableDictionary.layerColorIndex = layerColorIndex;

                Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行设备开关
        /// </summary>
        private static void ExecuteEquipmentSwitch()
        {
            // 实现设备开关的逻辑
        }

        /// <summary>
        /// 执行设备表导入
        /// </summary>
        private static void ExecuteEquipmentImport()
        {
            // 实现设备表导入的逻辑
        }

        /// <summary>
        /// 执行设备表导出
        /// </summary>
        private static void ExecuteEquipmentExport()
        {
            // 实现设备表导出的逻辑
        }

        /// <summary>
        /// 执行区域开关
        /// </summary>
        private static void ExecuteAreaSwitch()
        {
            // 实现区域开关的逻辑
        }

        #endregion

        /// <summary>
        /// 执行建筑命令的通用方法
        /// </summary>
        private static void ExecuteBuildingCommand(string commandName, string btnFileName, string btnBlockLayer, int layerColorIndex)
        {
            try
            {
                VariableDictionary.entityRotateAngle = 0;
                VariableDictionary.btnFileName = btnFileName;
                VariableDictionary.btnBlockLayer = btnBlockLayer;
                VariableDictionary.layerColorIndex = layerColorIndex;

                if (commandName == "吊顶" || commandName == "不吊顶")
                {
                    Env.Document.SendStringToExecute("Line2Polyline ", false, false, false);
                }
                else
                {
                    Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行建筑命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行吊顶命令
        /// </summary>
        private static void ExecuteCeilingCommand(bool isCeiling)
        {
            try
            {
                VariableDictionary.entityRotateAngle = 0;
                if (isCeiling)
                {
                    VariableDictionary.diaoDingHeight = TextBoxValueManager.GetTextBoxValue("textBox_TJ_JZ_height");
                    VariableDictionary.btnFileName = "JZTJ_吊顶";
                    VariableDictionary.btnBlockLayer = "TJ(建筑吊顶)";
                    VariableDictionary.layerColorIndex = 30;
                    Env.Document.SendStringToExecute("Line2Polyline ", false, false, false);
                }
                else
                {
                    VariableDictionary.btnFileName = "JZTJ_不吊顶";
                    VariableDictionary.btnBlockLayer = "TJ(建筑吊顶)";
                    VariableDictionary.layerColorIndex = 30;
                    Env.Document.SendStringToExecute("Line2Polyline ", false, false, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行吊顶命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行房间编号命令
        /// </summary>
        private static void ExecuteRoomNumberCommand()
        {
            try
            {
                VariableDictionary.entityRotateAngle = 0;
                // 从TextBox中获取房间编号信息
                string floorNo = TextBoxValueManager.GetTextBoxValue("楼层"); // 需要根据实际TextBox名称调整
                string cleanArea = TextBoxValueManager.GetTextBoxValue("洁净区");
                string systemArea = TextBoxValueManager.GetTextBoxValue("系统区");
                string roomSubNo = TextBoxValueManager.GetTextBoxValue("房间号");

                VariableDictionary.btnFileName = $"{floorNo}-{cleanArea}{systemArea}{roomSubNo}";
                VariableDictionary.btnBlockLayer = "TJ(房间编号)";
                VariableDictionary.layerColorIndex = 64; // 默认颜色

                Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);

                // 更新房间号
                UpdateRoomNumber(roomSubNo);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行房间编号命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新房间号
        /// </summary>
        private static void UpdateRoomNumber(string currentNumber)
        {
            // 实现房间号自增逻辑
        }
        /// <summary>
        /// 执行排水沟命令
        /// </summary>
        private static void ExecuteDrainageCommand()
        {
            try
            {
                VariableDictionary.entityRotateAngle = 0;
                VariableDictionary.btnFileName = "JZTJ_排水沟";
                VariableDictionary.buttonText = "JZTJ_排水沟";
                VariableDictionary.btnBlockLayer = "TJ(建筑专业J)";
                VariableDictionary.layerColorIndex = 30;

                // 从TextBox获取排水沟尺寸
                VariableDictionary.dimString_JZ_宽 = TextBoxValueManager.GetTextBoxValue("排水沟宽");
                VariableDictionary.dimString_JZ_深 = TextBoxValueManager.GetTextBoxValue("排水沟深");

                Env.Document.SendStringToExecute("Rec2PolyLine_3 ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行排水沟命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行建筑墙体开洞命令
        /// </summary>
        private static void ExecuteBuildingWallHoleCommand(string wallType)
        {
            try
            {
                VariableDictionary.buttonText = $"JZTJ_{wallType}开洞";
                VariableDictionary.textbox_RecPlus_Text = TextBoxValueManager.GetTextBoxValue($"{wallType}加宽"); // 获取加宽值
                VariableDictionary.layerColorIndex = 30;

                Env.Document.SendStringToExecute("Rec2PolyLine ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行建筑墙体开洞命令时出错: {ex.Message}");
            }
        }


        /// <summary>
        /// 执行结构受力点命令
        /// </summary>
        private static void ExecuteStructureLoadPointCommand()
        {
            try
            {
                VariableDictionary.entityRotateAngle = 0;
                VariableDictionary.btnFileName = "TJ(结构专业JG)";
                VariableDictionary.btnFileName_blockName = "A$C9bff4efc";
                VariableDictionary.btnBlockLayer = "TJ(结构专业JG)";
                VariableDictionary.buttonText = "STJ_受力点";
                VariableDictionary.layerColorIndex = 231;
                VariableDictionary.dimString = TextBoxValueManager.GetTextBoxValue("textBox_荷载数据");
                VariableDictionary.resourcesFile = Resources.STJ_受力点;
                VariableDictionary.blockScale = 1;
                Env.Document.SendStringToExecute("GB_InsertBlock_5 ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行结构受力点命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行水平荷载命令
        /// </summary>
        private static void ExecuteHorizontalLoadCommand()
        {
            try
            {
                VariableDictionary.btnFileName = "TJ(结构专业JG)";
                VariableDictionary.btnBlockLayer = "TJ(结构专业JG)";
                VariableDictionary.buttonText = "STJ_水平荷载";
                VariableDictionary.dimString = TextBoxValueManager.GetTextBoxValue("textBox_荷载数据");
                VariableDictionary.layerColorIndex = 231;
                Env.Document.SendStringToExecute("NLinePolyline_N ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行水平荷载命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行面着地命令
        /// </summary>
        private static void ExecuteSurfaceGroundCommand()
        {
            try
            {
                VariableDictionary.btnFileName = "TJ(结构专业JG)";
                VariableDictionary.btnBlockLayer = "TJ(结构专业JG)";
                VariableDictionary.buttonText = "STJ_面着地";
                VariableDictionary.dimString = TextBoxValueManager.GetTextBoxValue("textBox_荷载数据");
                VariableDictionary.layerColorIndex = 231;
                Env.Document.SendStringToExecute("NLinePolyline ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行面着地命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行框着地命令
        /// </summary>
        private static void ExecuteFrameGroundCommand()
        {
            try
            {
                VariableDictionary.btnFileName = "TJ(结构专业JG)";
                VariableDictionary.btnBlockLayer = "TJ(结构专业JG)";
                VariableDictionary.buttonText = "STJ_框着地";
                VariableDictionary.layerColorIndex = 231;
                VariableDictionary.dimString = TextBoxValueManager.GetTextBoxValue("textBox_荷载数据");
                Env.Document.SendStringToExecute("NLinePolyline_Not ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行框着地命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行圆形开洞命令
        /// </summary>
        private static void ExecuteCircularHoleCommand()
        {
            try
            {
                VariableDictionary.btnFileName = "TJ(结构专业JG)";
                VariableDictionary.btnBlockLayer = "TJ(结构专业JG)";
                VariableDictionary.buttonText = "STJ_圆形开洞";
                VariableDictionary.textBox_S_CirDiameter = Convert.ToDouble(TextBoxValueManager.GetTextBoxValue("textBox_S_直径"));
                VariableDictionary.textbox_CirPlus_Text = TextBoxValueManager.GetTextBoxValue("textBox_cirDiameter_Plus");
                VariableDictionary.layerColorIndex = 231;

                if (VariableDictionary.textBox_S_CirDiameter == 0)
                {
                    Env.Document.SendStringToExecute("CirDiameter ", false, false, false);
                }
                else
                {
                    Env.Document.SendStringToExecute("CirDiameter_2 ", false, false, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行圆形开洞命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行半径开圆洞命令
        /// </summary>
        private static void ExecuteRadiusHoleCommand()
        {
            try
            {
                VariableDictionary.btnFileName = "TJ(结构专业JG)";
                VariableDictionary.buttonText = "STJ_圆形开洞";
                VariableDictionary.textbox_S_Cirradius = Convert.ToDouble(TextBoxValueManager.GetTextBoxValue("textBox_S_半径"));
                VariableDictionary.textbox_CirPlus_Text = TextBoxValueManager.GetTextBoxValue("textBox_cirRadius_Plus");
                VariableDictionary.layerColorIndex = 231;

                if (VariableDictionary.textbox_S_Cirradius == 0)
                {
                    Env.Document.SendStringToExecute("CirRadius ", false, false, false);
                }
                else
                {
                    Env.Document.SendStringToExecute("CirRadius_2 ", false, false, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行半径开圆洞命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行矩形开洞命令
        /// </summary>
        private static void ExecuteRectangularHoleCommand()
        {
            try
            {
                VariableDictionary.textbox_Height = TextBoxValueManager.GetTextBoxValue("textBox_S_高");
                VariableDictionary.textbox_Width = TextBoxValueManager.GetTextBoxValue("textBox_S_宽");
                VariableDictionary.btnBlockLayer = "TJ(结构专业JG)";
                VariableDictionary.buttonText = "矩形开洞";
                VariableDictionary.layerColorIndex = 231;

                double height = Convert.ToDouble(VariableDictionary.textbox_Height);
                double width = Convert.ToDouble(VariableDictionary.textbox_Width);

                if (height > 0 && width > 0)
                {
                    // 设置全局变量
                    //SetGlobalVariable("recAndMRec", 0);
                    VariableDictionary.btnFileName = "TJ(结构专业JG)";
                    VariableDictionary.textbox_RecPlus_Text = TextBoxValueManager.GetTextBoxValue("textBox2_RectangleExpansion");
                    Env.Document.SendStringToExecute("DrawRec ", false, false, false);
                }
                else
                {
                    VariableDictionary.btnFileName = "TJ(结构专业JG)";
                    VariableDictionary.textbox_RecPlus_Text = TextBoxValueManager.GetTextBoxValue("textBox2_RectangleExpansion");
                    Env.Document.SendStringToExecute("Rec2PolyLine ", false, false, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行矩形开洞命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行给排水命令的通用方法
        /// </summary>
        private static void ExecutePlumbingCommand(string commandName, string btnFileName, string blockName, string layerName, int colorIndex, byte[] resourceFile)
        {
            try
            {
                VariableDictionary.entityRotateAngle = 0;
                VariableDictionary.btnFileName = btnFileName;
                VariableDictionary.btnFileName_blockName = blockName;
                VariableDictionary.btnBlockLayer = layerName;
                VariableDictionary.layerColorIndex = colorIndex;
                VariableDictionary.resourcesFile = resourceFile;

                if (resourceFile != null)
                {
                    Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
                }
                else
                {
                    Env.Document.SendStringToExecute("DDimLinearP ", false, false, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行给排水命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行暖通命令的通用方法
        /// </summary>
        private static void ExecuteHVACCommand(string commandName, string btnFileName, string layerName, int colorIndex)
        {
            try
            {
                VariableDictionary.entityRotateAngle = 0;
                VariableDictionary.btnFileName = btnFileName;
                VariableDictionary.btnBlockLayer = layerName;
                VariableDictionary.layerColorIndex = colorIndex;
                Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行暖通命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行暖通百分比命令
        /// </summary>
        private static void ExecuteHVACPercentageCommand()
        {
            try
            {
                string percentageText = TextBoxValueManager.GetTextBoxValue("textBox_排风百分比");
                if (percentageText == "排风百分比")
                {
                    VariableDictionary.btnFileName = "(排风 ？ %)";
                }
                else
                {
                    VariableDictionary.btnFileName = $"(排风 {percentageText} %)";
                }

                Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
                ExecuteHVACCommand("排风百分比", VariableDictionary.btnFileName, "TJ(暖通专业N)", 6);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行暖通百分比命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行电气命令的通用方法
        /// </summary>
        private static void ExecuteElectricalCommand(string commandName, string btnFileName, string blockName, string layerName, int colorIndex, byte[] resourceFile, int scale)
        {
            try
            {
                VariableDictionary.entityRotateAngle = 0;
                VariableDictionary.btnFileName = btnFileName;
                VariableDictionary.btnFileName_blockName = blockName;
                VariableDictionary.btnBlockLayer = layerName;
                VariableDictionary.layerColorIndex = colorIndex;
                VariableDictionary.resourcesFile = resourceFile;
                VariableDictionary.blockScale = scale;

                if (scale == 500)
                {
                    Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
                }
                else
                {
                    Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行电气命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行自控命令的通用方法
        /// </summary>
        private static void ExecuteControlCommand(string commandName, string btnFileName, string blockName, string layerName, int colorIndex, byte[] resourceFile, int scale)
        {
            try
            {
                VariableDictionary.entityRotateAngle = 0;
                VariableDictionary.btnFileName = btnFileName;
                VariableDictionary.btnFileName_blockName = blockName;
                VariableDictionary.btnBlockLayer = layerName;
                VariableDictionary.layerColorIndex = colorIndex;
                VariableDictionary.resourcesFile = resourceFile;
                VariableDictionary.blockScale = scale;

                if (scale == 500)
                {
                    Env.Document.SendStringToExecute("GB_InsertBlock_2 ", false, false, false);
                }
                else
                {
                    Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行自控命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取按钮对应的执行方法
        /// </summary>
        /// <param name="buttonName">按钮名称</param>
        /// <returns>执行方法，如果未找到则返回null</returns>
        public static Action GetCommandForButton(string buttonName)
        {
            // 检查按钮名称是否在字典中
            if (_buttonCommandMap.ContainsKey(buttonName))
            {
                // 返回按钮对应的执行方法
                return _buttonCommandMap[buttonName];
            }
            return null;// 未找到按钮名称，返回null
        }

        /// <summary>
        /// 检查是否是预定义按钮
        /// </summary>
        /// <param name="buttonName">按钮名称</param>
        /// <returns>true表示是预定义按钮，false表示是动态图元按钮</returns>
        public static bool IsPredefinedButton(string buttonName)
        {
            return _buttonCommandMap.ContainsKey(buttonName);
        }

        /// <summary>
        /// 添加自定义按钮映射
        /// </summary>
        /// <param name="buttonName">按钮名称</param>
        /// <param name="command">执行方法</param>
        public static void AddCustomButtonMapping(string buttonName, Action command)
        {
            if (!_buttonCommandMap.ContainsKey(buttonName))
            {
                _buttonCommandMap[buttonName] = command;
            }
        }


        /// <summary>
        /// 设置全局变量（需要根据实际实现方式调整）
        /// </summary>
        private static void SetGlobalVariable(string variableName, object value)
        {
            // 这里需要根据实际的全局变量访问方式来实现
        }

    }
    /// <summary>
    /// TextBox值管理器
    /// </summary>
    public static class TextBoxValueManager
    {
        // 存储TextBox值的字典
        private static readonly Dictionary<string, string> _textBoxValues = new Dictionary<string, string>();

        /// <summary>
        /// 设置TextBox的值
        /// </summary>
        public static void SetTextBoxValue(string textBoxName, string value)
        {
            _textBoxValues[textBoxName] = value;
        }

        /// <summary>
        /// 获取TextBox的值
        /// </summary>
        public static string GetTextBoxValue(string textBoxName)
        {
            if (_textBoxValues.ContainsKey(textBoxName))
            {
                return _textBoxValues[textBoxName];
            }
            return "";
        }

        /// <summary>
        /// 清除所有TextBox值
        /// </summary>
        public static void ClearAllValues()
        {
            _textBoxValues.Clear();
        }
    }
}
