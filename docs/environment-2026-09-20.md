# 阶段 0 环境核实报告

> 核实日期：2026-09-20  
> 核实人：主 Agent（只读检查 + 真实安装验证）  
> 结论先行：开发工具链已具备；**无可用的 AutoCAD 运行环境、无真实图集样本、无云端凭据**，相关验收暂时无法执行，已如实记录。

## 1. 已核实的既有环境

| 项目 | 核实结果 | 核实方式 |
| --- | --- | --- |
| 操作系统 | Windows 11 专业版，10.0.26200 | PowerShell OSVersion / Win32_OperatingSystem |
| Git | 2.x（`C:\Program Files\Git`） | `where git` |
| .NET Framework 运行时 | 4.8.09221（Release 533509） | 注册表 NDP\v4\Full |
| 网络 | 可达（dot.net、download.microsoft.com） | curl 实测 |

## 2. 本阶段完成的环境准备

| 项目 | 结果 | 说明 |
| --- | --- | --- |
| .NET SDK | **8.0.425 x64 已安装**（winget，官方源，哈希已验证） | `dotnet --list-sdks` 实测 |
| Git 仓库 | 已初始化（main 分支），文档基线已提交 | `git log` 实测 |

安装过程问题记录：winget 首次因 msstore 源不可达失败，改用 `--source winget` 后成功（见 devlog 2026-09-20 节点 001）。

## 3. 缺失项与影响（不得假设存在）

| 缺失项 | 影响 | 处置 |
| --- | --- | --- |
| AutoCAD 主程序 | 阶段 1 CAD 真机验证、A07/A08 验收无法执行 | 继续可独立完成的工作；版本矩阵全部标记“未构建、未实测”；请用户提供可测试 CAD |
| 代表图集 PDF（3～10 本） | 阶段 3 导入识别、95% 指标评测无法执行 | 请用户提供；在此之前用合成/最小测试资料只做流程验证 |
| 云端识别凭据（Qwen） | 真实识别调用无法执行 | 仅在使用前配置；不写入代码与日志 |
| .NET Framework 目标包（Reference Assemblies） | net48 构建需从 NuGet 恢复 `Microsoft.NETFramework.ReferenceAssemblies` | 已在构建中实测验证（见下） |

## 4. 待验证但本机可先执行的技术验证

1. **net48 WPF 可由 .NET SDK 8 构建**：SDK 会自动从 NuGet 恢复引用程序集；需实测一次构建确认。
2. SQLite 驱动对 net48 的支持（System.Data.SQLite 或 Microsoft.Data.Sqlite）。
3. 命名管道与后续 CAD 协议（无 CAD 时仅验证协议层）。

## 5. 测试资料清单约定（待用户提供真实样本前）

- 不允许把模拟数据混入正式资料库；测试一律使用独立测试库与合成样本。
- 真实图集到手后，按 PRD 第 10 节建立 ≥100 条人工对照集，固定匹配规则后再评测。
- 云凭据只在确需真实识别调用时配置，使用 DPAPI CurrentUser 保存。
