# 稳定 ID 与命名规范

## 原则

显示名称可本地化和调整，稳定 ID 一经进入正式存档/录像不得随意变化。

## 建议格式

- 基础体系：`FACTION_ALLIED`、`FACTION_CN`、`FACTION_EURASIA`、`FACTION_YURI`
- 国家：`COUNTRY_CN`、`COUNTRY_US` 等
- 单位：`UNIT_CN_DRAGON_MBT`
- 建筑：`BLDG_AI_COMMAND_CENTER`
- 武器：`WPN_120MM_AP`、`WPN_LASER_CIWS`
- 科技：`TECH_T3_ROBOTICS`
- 能力：`ABILITY_CN_SKYDOME`
- 地图：`MAP_GREYFIELD_1V1`
- 任务：`MISSION_PROLOGUE_01`

## 资源引用

代码/数据/测试/美术挂点/音效事件尽量通过同一稳定 ID 或其生成的强类型句柄关联，禁止同一对象在各部门拥有不同自由文本名称。

## 版本

数据格式采用显式 schema 版本；网络协议、录像格式、存档格式分别版本化。兼容规则由迁移器维护。