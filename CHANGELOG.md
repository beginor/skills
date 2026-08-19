# Changelog

## 2026-08-19

- **sharp-db**
  - `execute` 新增 `--yes` 非交互开关，Claude Code / 脚本环境下可自动运行 SQL 文件
  - `query` 新增 `--limit <n>` 行数限制（默认 100，`0` 关闭），防止大结果集撑爆上下文，截断时输出提示
  - 所有命令新增 `--connection-env <VAR>`，从环境变量读取连接串，避免明文密码出现在命令行
  - SKILL.md 工作流改为推断优先：从上下文自动推断数据库类型与连接串，仅在无法确定时询问用户
  - 新增多语句 SQLite 文件执行的回归测试，验证 Microsoft.Data.Sqlite 原生批执行行为

## 2026-08-04

- **sharp-db**
  - columns 命令新增 `character_maximum_length` 列，返回字符串类型（char/varchar）的最大长度；PostgreSQL 额外支持 `varchar[]` 数组元素长度提取
  - 文档完善：CLAUDE.md / README.md / SKILL.md 同步 column length 说明，README 修正项目路径

## 2026-07-26

- Add license and readme
- **sharp-db**
  - Move to sharp-db subdirectory
  - add license file

