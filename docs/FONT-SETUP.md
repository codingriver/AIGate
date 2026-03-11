# Unity UI Toolkit 字体配置完整指南

> 适用版本：Unity 2022.x / UI Toolkit  
> 项目字体：思源黑体（Source Han Sans SC Bold）支持简中 / 日文 / 韩文 CJK 字符

---

## 一、背景与限制说明

### Unity USS 不支持的写法

```css
/* ❌ 错误：Unity USS 不支持 system-font() 函数，任何版本均不支持 */
-unity-font-definition: system-font('Microsoft YaHei'), system-font('Arial');

/* ❌ 错误：不支持 CSS 字体回退链 */
-unity-font-definition: url('a.asset'), url('b.asset');
```

### Unity USS 支持的写法

```css
/* ✅ 正确：单个 FontAsset，使用 url() 或 resource() */
-unity-font-definition: url("/Assets/Fonts/SourceHanSansSC-Bold SDF.asset");

/* ✅ 正确：FontAsset 在 Assets/Resources/ 下时可用 resource() */
-unity-font-definition: resource("Fonts/SourceHanSansSC-Bold SDF");
```

**结论：`-unity-font-definition` 只接受单个 FontAsset 引用，不支持字体回退链。  
多语言字体回退需在 PanelSettings → TextSettings 的 Fallback Font Assets 中配置。**

---

## 二、项目现有字体资产

| 文件 | 类型 | 路径 |
|------|------|------|
| `SourceHanSansSC-Bold.otf` | 原始字体文件 | `Assets/Fonts/` |
| `SourceHanSansSC-Bold SDF.asset` | TMP FontAsset（已编译） | `Assets/Fonts/` |
| `TextSettings.asset` | UI Toolkit 文字设置 | `Assets/` |
| `PanelSettings.asset` | UI Document 面板设置 | `Assets/Resources/` |

`TextSettings.asset` 已被 `PanelSettings.asset` 引用（`textSettings` 字段），链路完整。

---

## 三、字体回退配置方式（替代 system-font）

Unity UI Toolkit 的多语言字体回退通过 **TextSettings → Fallback Font Assets** 实现，  
相当于 CSS 的 `font-family` 回退链，Unity 会按顺序查找字符。

### 3.1 通过 Inspector 配置（推荐）

1. 在 Project 窗口找到 `Assets/TextSettings.asset`
2. 在 Inspector 中找到 **Fallback Font Assets** 列表
3. 点击 **+** 添加备用 FontAsset（如需要支持更多语言）
4. 拖入对应语言的 FontAsset，Unity 渲染时找不到字符会自动 fallback

当前 `TextSettings.asset` 已配置的 Fallback：
- guid `53a21e9bf9b361e4ca8229a26aebae13`（已有回退字体1）
- guid `8b3b99e106aaeb4449c8fcfac5f47e8c`（Default Font Asset）

### 3.2 如需扩展更多语言字体

推荐下载 [Noto Sans 系列](https://fonts.google.com/noto)（Google 免费，覆盖几乎所有语言）：

| 字体文件 | 覆盖语言 |
|----------|----------|
| NotoSansSC-Regular.otf | 简体中文 |
| NotoSansJP-Regular.otf | 日文 |
| NotoSansKR-Regular.otf | 韩文 |
| NotoSansThai-Regular.otf | 泰文 |

导入步骤见下方第四节。

---

## 四、新增 FontAsset 完整流程

> 如果项目已有的 `SourceHanSansSC-Bold SDF.asset` 满足需求，跳过本节。

### Step 1：导入字体文件

1. 将 `.ttf` 或 `.otf` 文件拖入 `Assets/Fonts/` 目录
2. Unity 自动生成 `.meta` 文件，无需额外操作

### Step 2：生成 TMP FontAsset

1. 菜单：**Window → TextMeshPro → Font Asset Creator**
2. 配置参数：

| 参数 | 推荐值 | 说明 |
|------|--------|------|
| Source Font File | 你的 .otf/.ttf | 选择导入的字体文件 |
| Sampling Point Size | Auto Sizing | 自动适配 |
| Atlas Resolution | 4096 × 4096 | CJK 字符集建议用大图集 |
| Character Set | Custom Characters | 或选 Chinese (Simplified) |
| Render Mode | SDF32 | 矢量字体，缩放不模糊 |

3. 点击 **Generate Font Atlas** 等待生成
4. 点击 **Save** 保存到 `Assets/Fonts/`，文件名如 `MyFont SDF.asset`

### Step 3：添加到 TextSettings Fallback

1. 打开 `Assets/TextSettings.asset`
2. 在 **Fallback Font Assets** 中点击 **+**
3. 将新生成的 `.asset` 拖入

---

## 五、USS 文件正确写法

### GlobalFont.uss（当前配置）

```css
:root {
    /*
     * Unity USS 中 -unity-font-definition 只接受单个 FontAsset 引用，
     * 不支持 system-font() 或字体回退链。
     *
     * 多语言回退通过 Assets/TextSettings.asset → Fallback Font Assets 配置。
     * PanelSettings.asset 已引用 TextSettings.asset，链路完整。
     */
    -unity-font-definition: url("/Assets/Fonts/SourceHanSansSC-Bold SDF.asset");
}
```

### 在具体元素上覆盖字体

```css
/* 某个元素使用不同字体 */
.my-label {
    -unity-font-definition: resource("Fonts/AnotherFont SDF");
}
```

> **注意**：`resource()` 路径从 `Assets/Resources/` 开始，不含扩展名。  
> `url()` 路径从项目根目录开始，需含扩展名 `.asset`。

---

## 六、PanelSettings 与 TextSettings 的关系

```
UI Document (.uxml)
    └── PanelSettings.asset          ← 挂在 UIDocument 组件上
            ├── themeUss             ← 主题样式（GateTheme.uss 等）
            └── textSettings         ← 指向 TextSettings.asset
                    ├── m_DefaultFontAsset    ← 默认字体
                    └── m_FallbackFontAssets  ← 多语言回退字体列表
```

当 UI Toolkit 渲染文字时：
1. 优先使用 USS 中 `-unity-font-definition` 指定的字体
2. 找不到字符时，按顺序查 `TextSettings.m_FallbackFontAssets`
3. 全部找不到时显示方块 □

---

## 七、验证字体生效

1. 在 Editor 中运行场景
2. 输入中文字符，检查是否正常显示（无方块）
3. 如出现方块：
   - 检查 FontAsset 的 **Character Table** 是否包含该字符（Inspector 可查）
   - 若字符集不完整，在 Font Asset Creator 中重新生成，扩大字符集
   - 或将该字符所在语言的 FontAsset 添加到 Fallback 列表

---

## 八、常见问题

### Q: USS 设置了字体但不生效？
- 确认 `url()` 路径正确，注意文件名中的空格（如 `SourceHanSansSC-Bold SDF.asset`）
- 确认 UIDocument 组件挂载了正确的 PanelSettings
- 检查是否有更高优先级的样式覆盖

### Q: Window → TextMeshPro → Settings 菜单不存在？
- Unity 2022 UI Toolkit 使用的是独立的 `TextSettings.asset`，不是 TMP 的 `TMP_Settings`
- 直接在 Project 窗口找到并选中 `Assets/TextSettings.asset` 在 Inspector 中编辑
- 或通过 `Assets/Resources/PanelSettings.asset` → Inspector 中的 `Text Settings` 字段点击跳转

### Q: Font Asset Creator 在哪里？
- **Window → TextMeshPro → Font Asset Creator**（需要已安装 TextMeshPro 包）
- 如果菜单不存在：**Window → Package Manager** → 搜索 TextMeshPro → Install

### Q: 思源黑体是否覆盖日韩文字？
- 是的，`SourceHanSansSC-Bold.otf`（思源黑体简体）包含：
  - CJK 统一汉字（简体中文、繁体中文、日文汉字、韩文汉字）
  - 平假名、片假名（日文）
  - 韩文字母（Hangul）部分覆盖
  - 基本拉丁字母
- 不包含：泰文、阿拉伯文等非 CJK 文字（需额外 Fallback FontAsset）
