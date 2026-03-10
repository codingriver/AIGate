# Gate UI 界面布局与交互说明

主题: 深色科技风  bg #0f1117 / accent #4f8ef7 / 字体: SimHei

---

## 一、主窗口 GateMainWindow.uxml

```
+----------------------------------------------+
| GATE  代理配置管理                  v1.0       |
+----------+-----------------------------------+
| 侧边导航  |  内容区 content-area              |
| [G] 全局代理  <- 选中:蓝字+左竖条            |
| [A] 应用代理                                |
| [P] 预  设                                 |
| [S] 状态总览                                |
| [T] 连通测试                                |
|  -----                                      |
| [W] 配置向导  <- 紫色                        |
+----------+-----------------------------------+
```

导航: 默认灰 -> hover背景#1e2436 -> 选中背景#1e2d4f+蓝字+左边框3px
切换面板时调用对应控制器 Refresh()

---

## 二、全局代理 GlobalPanel.uxml

默认状态:
- 状态卡三行均显示 "(未设置)" 灰色斜体
- 所有输入框为空
- toggle未勾选

布局:
  [状态卡] HTTP_PROXY / HTTPS_PROXY / NO_PROXY 当前值
  [表单卡] 代理地址/HTTP/HTTPS/NO_PROXY 输入框 + 测试toggle
  [操作行] 清除代理(红) | 刷新状态(次要) | 应用设置(主要)
  [反馈标签]

状态变化:
  应用设置 -> 状态卡数值变黄色, 反馈绿色成功
  清除代理 -> 状态卡变灰斜体, 反馈蓝色提示
  验证失败 -> 反馈红色错误信息

---

## 三、应用代理 AppPanel.uxml

布局:
  [工具栏] 搜索输入框 | 分类下拉 | 仅已安装toggle
  [列表头] 应用名称 | 分类 | 状态 | 操作
  [ListView] 每行44px, 应用名/分类/状态点/设置+清除按钮
  [批量栏] 已选N个 | 批量代理输入框 | 全选已安装 | 批量设置 | 批量清除
  [反馈标签]

状态点: 绿#22c55e已配置 / 灰#334155未配置
搜索: 实时过滤, 不区分大小写
分类下拉: 全部/版本控制/包管理器/AI IDE等

弹窗 edit-overlay (默认display:none):
  触发: 点击列表行[设置]按钮
  内容: 标题"编辑: {appname}" + 代理地址输入框
  取消: 关闭弹窗不保存
  保存: 写入配置, 关闭弹窗, 刷新状态点

---

## 四、预设配置集 PresetPanel.uxml

布局:
  [左列 210px] 预设ListView + [+新建预设]按钮
  [右列 flex] 详情区: 名称/创建/更新/HTTP值(黄)/应用数 + 操作按钮组
  [保存栏] 预设名称输入框 + 保存按钮
  [反馈标签]

行为:
  点击预设项 -> 右侧详情更新
  应用 -> 加载配置, 绿色反馈
  设为默认 -> 名称后加"(默认)"
  删除 -> 列表移除, 详情清空

弹窗 new-preset-overlay (默认display:none):
  触发: 点击[+新建预设]
  内容: 预设名称输入框 + 描述输入框
  取消: 关闭
  创建: 保存当前全部配置为新预设, 刷新列表

---

## 五、状态总览 StatusPanel.uxml

布局:
  [全局代理卡] HTTP/HTTPS/NO_PROXY值 + 各行[编辑]按钮  右上角[刷新]
  [应用代理卡] 已配置N个徽章 + 可滚动工具列表(按分类分组)
  [当前预设卡] 预设名称蓝色粗体

行为:
  刷新 -> 重读所有数据
  编辑 -> 跳转全局代理面板聚焦对应输入框
  工具列表C#动态生成: 分类标题+工具行(绿点+名称+代理值)

---

## 六、连通性测试 TestPanel.uxml

布局:
  [表单卡] 代理地址输入框+[使用当前]按钮 / 测试URL输入框
  [大按钮] 开始测试
  [结果区]
    success卡片(默认display:none): OK连接成功 + 响应时间 + 目标URL
    fail卡片(默认display:none): ERR连接失败 + 错误信息
  [历史卡] 测试历史ListView max-height 180px

行为:
  使用当前 -> 读取环境变量填入代理输入框
  开始测试 -> 按钮显示"测试中..."禁用, 结果卡全部隐藏
  测试成功 -> success卡display:flex, 填写时间/URL, 历史追加绿条目
  测试失败 -> fail卡display:flex, 填写错误信息, 历史追加红条目
  按钮恢复可用

---

## 七、控件样式覆盖

gate-input (TextField):
  背景#0a0e18 边框#1e293b 圆角6px 高34px
  focus: 边框#4f8ef7 背景#0d1626
  placeholder: 灰#334155 斜体

gate-toggle (Toggle):
  自定义18x18复选框 圆角4px
  未选: 边框#334155 背景#0a0e18
  选中: 背景#4f8ef7 边框#4f8ef7

gate-dropdown (DropdownField):
  输入区背景#0a0e18 边框#1e293b 圆角6px 高34px
  hover: 边框蓝
  下拉菜单: 背景#111827 圆角8px
  item hover: 背景#1e2d4f 蓝字

gate-listview (ListView):
  背景#0a0e18 圆角8px 边框#1e293b
  item hover: #131f35
  item selected: #1e2d4f
  滚动条8px 隐藏按钮

按钮:
  btn-primary: 背景#4f8ef7 白字 hover#6ba3f9
  btn-secondary: 背景#1e293b 灰字 hover边框蓝
  btn-danger: 透明背景 红字#f87171 红边框
  btn-sm: 高30px
  btn-large: 高44px

## 八、弹窗通用规则

所有overlay默认display:none
C#通过 element.style.display = DisplayStyle.Flex 显示
遮罩: position:absolute 覆盖全面板 rgba(0,0,0,0.75)
面板: 宽460px 背景#111827 圆角14px padding 28x32px
按钮组: 右对齐, 取消在左(次要), 确认在右(主要)
