# 必备资料（带到公司电脑用）

> 本目录是“建筑做法资料库”项目在另一台 Windows 电脑上试验所需的全部材料。
> 整理日期：2026-09-21。对应代码版本：Git 提交 `6f960a8`（节点 014）。

## 目录内容

| 文件/文件夹 | 是否必备 | 用途 |
| --- | --- | --- |
| `dotnet-sdk-8.0.425-win-x64.exe` | 必备（仅编译需要） | .NET 8 SDK 安装包，本项目实测版本 8.0.425，官方 Microsoft 下载 |
| `Tuku.Desktop-发布版/` | 必备 | 已构建好的桌面程序（Release, win-x64），双击即可运行，无需安装 SDK |
| `本说明文件` | 必读 | 安装与运行步骤 |

源代码在 GitHub：https://github.com/shjyky1995-cmyk/tukustory （本目录只需拷贝，不需要联网）。

## 方案一：直接运行发布版（最快，推荐先试这个）

适用：只想试用软件功能，不需要改代码。

1. 把 `Tuku.Desktop-发布版` 整个文件夹拷贝到公司电脑（保持文件夹完整，里面有 21 个文件，含 `Tuku.Desktop.exe` 和 `e_sqlite3.dll`，缺一个都无法启动）。
2. 双击 `Tuku.Desktop.exe`。
3. 首次启动会自动在 `%LOCALAPPDATA%\Tuku\Library\` 建立本地资料库（library.db），并写入默认分类（国标、楼地面、内墙面、外墙面、屋面、顶棚、其他/待分类）。

系统要求：Windows 10/11 64 位；.NET Framework 4.8 运行时（Win10 1903 之后和 Win11 均自带，一般无需安装）。

## 方案二：从源代码构建运行

适用：需要查看或修改代码。

1. 安装 `dotnet-sdk-8.0.425-win-x64.exe`（双击，一路下一步；或命令行 `dotnet-sdk-8.0.425-win-x64.exe /install /quiet /norestart`）。
2. 获取源代码（二选一）：
   - 公司网络可访问 GitHub：`git clone https://github.com/shjyky1995-cmyk/tukustory.git`
   - 或从 U 盘拷贝整个项目目录（建议先删除各项目下的 `bin`、`obj` 文件夹）。
3. 在项目根目录执行：
   ```bash
   dotnet build Tuku.sln
   dotnet run --project src/Tuku.Desktop
   ```
4. 运行测试（可选）：
   ```bash
   dotnet test tests/Tuku.UnitTests
   dotnet test tests/Tuku.IntegrationTests
   ```

## 试用路径建议

1. 菜单“手动新建”→ 填名称和至少一行层次 → 保存
2. 中间栏输入关键词搜索（支持中文、编号；编号精确匹配排最前）
3. 点开结果 → “编辑”修改 → 保存（会生成新修订并自动变为“未核对”）
4. 右下角“修订历史”选择旧版本 → “恢复所选修订”（生成新修订，不丢历史）
5. “复制纯文字”可把做法文字粘贴到 CAD 图纸（当前版本未接入 CAD 自动插入）

## 注意事项

- 资料库数据在每台电脑上独立存放（`%LOCALAPPDATA%\Tuku\Library\`），公司电脑上是空白新库，不影响家里电脑的数据。
- 当前版本不包含：PDF 导入与云识别（阶段 3，未开发）、CAD 自动插入（阶段 1，缺 AutoCAD 环境未验证）。“插入 CAD”按钮在无 CAD 环境会提示不可用。
- 安装包为 Microsoft 官方来源（本项目即用此版本构建并通过全部测试）；如公司有软件白名单要求，请提前报备 `dotnet-sdk-8.0.425-win-x64.exe`。
- 本目录内的安装包与发布版不纳入 Git 版本控制（体积大且属分发物），仅作本地拷贝用途。
