# 决策记录 0001：net48 原生依赖固定 win-x64 RID

- 日期：2026-09-20
- 状态：已采用并经构建与测试验证
- 关联节点：devlog 2026-09-20 节点 009

## 背景

架构选定桌面程序为 .NET Framework 4.8 64 位，本地数据库使用 SQLite（Microsoft.Data.Sqlite，netstandard2.0）。首次在 net48 进程中打开连接即失败。

## 问题现象与定位

1. 现象：`SqliteConnection` 静态构造抛 `TypeInitializationException`，内层 `Win32Exception 0x80004005 / 0x8007007E`（找不到指定的模块）或 `193`（不是有效的 Win32 应用程序）。
2. 定位过程：
   - 用 pefile 检查输出目录中的 `e_sqlite3.dll`：根目录平铺的是 **x86** 版本（只依赖 KERNEL32.dll，无外部依赖缺失问题）。
   - 无 RID 的 net48 构建把 x86 原生库平铺到输出根目录，x64 进程加载即 193 错误。
   - 测试工程（net48 xunit）另有第二个现象：测试宿主影子拷贝（assembly\dl3 临时目录）不复制 `runtimes/win-x64/native` 布局，导致同样加载失败（126）。
3. 结论：失败不是 SQLite 本身不可用，而是 net48 输出中原生库的平台布局与进程位数不匹配。

## 决策

- net48 项目（Tuku.Infrastructure、Tuku.IntegrationTests）固定 `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`，与架构确定的 64 位桌面一致；输出目录落在 `bin/<config>/net48/win-x64/`，原生库为正确位数。
- 集成测试通过 `xunit.runner.json` 设置 `shadowCopy: false`，避免测试宿主影子拷贝丢失原生库；不需要额外的 `--settings` 参数。

## 备选与拒绝理由

- 换用 System.Data.SQLite：同样是原生库部署问题，且引入 EF 无关的历史包，未解决根因。
- 在代码中手工 LoadLibrary：绕过标准部署机制，脆弱且不可移植，拒绝。
- 测试改用 net8.0：会失去对 net48 目标真实性的验证，拒绝（netstandard2.0 驱动虽可跨目标运行，但 net48 输出布局问题必须真实暴露）。

## 影响与验证

- 影响：构建输出路径变化（net48/win-x64）；部署时必须保持 x64 目录完整。32 位环境不在支持范围（架构已定 64 位）。
- 验证：`dotnet build Tuku.sln` 0 错误 0 警告；`dotnet test tests/Tuku.IntegrationTests` 10/10 通过（真实 SQLite 3.41.2）；临时 net48 控制台程序实测连接与建表成功。

## 遗留

- 未来引入 PDFium 等其他原生库时，必须先确认其 runtimes 布局在 win-x64 RID 下正确部署，再接入（见架构 2.2 PDF 行）。
