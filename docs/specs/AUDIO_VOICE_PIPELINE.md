# 音效、语音与音乐生产执行规格

本文件冻结音频实现与生产路径，补齐 `design/09_ART_UI_AUDIO.md` 和 `UI_ART_AUDIO_ACCESSIBILITY.md` 的执行层。

## 1. 技术结构

正式结构：

```text
游戏模拟/表现事件
→ AudioEventBus
→ WarAudioDirector
→ FMOD Studio 适配层
→ 平台音频设备
```

游戏逻辑禁止直接散落调用 FMOD 事件。所有音频通过 `AudioEventBus` 发送语义事件，由 `WarAudioDirector` 决定实际播放、聚合、虚拟化、丢弃和优先级。

FMOD 必须通过接口层隔离，避免未来中间件升级或替换牵动模拟代码。

## 2. 音频域

稳定域：

- `SFX_WEAPON`
- `SFX_VEHICLE`
- `SFX_ROBOT`
- `SFX_AIR`
- `SFX_NAVAL`
- `SFX_BUILDING`
- `SFX_UI`
- `SFX_ENV`
- `VOICE_UNIT`
- `VOICE_COMMAND`
- `VOICE_STORY`
- `MUSIC_DYNAMIC`
- `AMBIENCE_BATTLE`

## 3. 稳定 ID

音效：`SFX_<范围>_<对象>_<事件>_<三位变体>`。

语音：`VOICE_<语言或阵营>_<角色或兵种>_<事件>_<三位变体>`。

音乐：`MUSIC_<阵营或场景>_<状态>`。

示例：

```text
SFX_CN_DRAGON_MBT_FIRE_001
SFX_ROBOT_HEAVY_STEP_004
VOICE_CN_ARMOR_ATTACK_002
VOICE_AI_ZONE_WARNING_003
MUSIC_CN_BATTLE_HIGH
```

代码不得引用文件名。

## 4. 战场音频导演

`WarAudioDirector` 输入至少包含：

`event_id, position, faction, category, priority, intensity, source_count, listener_context`

优先级：

1. 当前选中单位/直接威胁；
2. 战略预警和任务语音；
3. 近距离战斗；
4. 中距离战斗聚合；
5. 远景战争层；
6. 环境；
7. 低优先装饰。

最高优先事件不能被普通炮火完全淹没。

## 5. 大规模声音聚合

禁止“一单位一完整声源”无限增长。

默认标准机预算：

- 逻辑音频事件：500—1000+可同时存在；
- 实际混音物理声道：目标96—128；
- 保留高优先声道：16—24；
- 超预算事件执行虚拟化、聚合或丢弃。

区域聚类 `BattleAudioCluster`：

- 近距8—12个最重要源保留独立声音；
- 中距同类事件按250—500m区域聚合；
- 远距转换为 `Distant Battle Layer`；
- 镜头接近时平滑从聚合层拆分为独立事件。

## 6. 武器声音分层

武器母版不得只有一个整段样本。推荐层：

```text
瞬态爆音
+ 低频冲击
+ 机械动作
+ 环境反射
+ 远距离尾音
```

每个核心武器至少4个可随机变体；普通同类武器至少3个。

未来武器按物理概念拆层：

- 磁暴：充能、电弧、低频冲击、放电尾音；
- 光棱：能量充电、束流释放、空气电离、棱镜共振；
- 神经武器：低频压力、人声/合成异常、相位变化；
- 机器人：伺服、电机、液压、关节冲击、步态、系统提示。

## 7. AI生成音效生产链

允许使用有明确商业许可的生成式音频服务生产普通音效候选。

流程：

```text
读取单位/武器规格
→ 生成 AudioAssetTask
→ 自动生成提示词
→ 每项生成8—16个候选
→ 自动质量检查
→ 人工/音频代理筛选
→ 分层编辑与混音
→ 响度/格式统一
→ 稳定ID入库
→ FMOD事件映射
→ 游戏内场景验收
```

自动质检至少检查：削波、异常直流、底噪、长度、静音段、频谱异常、重复度、明显人声污染（非语音资产）。

生成服务、模型版本、许可类型、生成时间和提示词必须写入资产来源元数据。

## 8. 母版格式

最终母素材统一：

- 48kHz；
- 24bit；
- WAV；
- 保留未压缩母版；
- 平台运行时压缩由 FMOD 构建配置负责。

禁止把有损压缩文件作为唯一母版。

## 9. 单位语音事件

每个功能单位最低语音池：

- SELECT：3；
- MOVE：3；
- ATTACK：3；
- UNDER_FIRE：2；
- DAMAGED：2；
- RETREAT：2；
- ABILITY：2；
- AI_TAKEOVER：2（适用）；
- COMMS_DEGRADED：2（适用）；
- COMMS_LOST：2（适用）。

国家特色单位在关键事件必须至少有一组独有语音。

同一语句默认60秒内避免连续重复；短确认句可采用8—20秒随机冷却。

## 10. 上下文语音选择

语音选择上下文：

`country, faction, unit_role, current_state, mission, damage_state, comms_state, autonomy, enemy_type, recent_voice_history`

语音系统输出已录制/已生成的合法句，不在竞技运行时自由生成长文本。

## 11. AI战斗指挥中心语音

采用“结构化语义 -> 已批准语句模板/片段”的方式。

输入例：

`zone=NORTH, threat=ARMOR, severity=HIGH, eta=120`

输出候选：

“北部发现大规模装甲集群，预计两分钟后接触。”

提供三种信息密度：简洁、标准、详细。玩家可独立关闭建议语音但保留关键战略警报字幕。

## 12. 真人与合成语音边界

- 主线核心角色、阵营重要指挥官、尤里核心人物：正式版优先真人配音；
- 普通单位、机器人、系统语音：允许使用明确授权的合成语音；
- 开发阶段：可用占位合成语音验证节奏；
- 禁止未经授权模仿现实人物或演员声线；
- 声音模型授权、演员合同、使用地域、期限和可再训练权限必须记录。

## 13. 本地化与原声模式

首发至少支持中文简体、英文完整结构。

模式：

- 本地化模式：重要角色/单位使用玩家语言；
- 原声战场模式：各国单位使用设定语言，字幕使用玩家语言。

语音 ID 与文本本地化键分离，不能通过拼接本地化句子生成语法不稳定对白。

## 14. 动态音乐

音乐状态：

`PEACE, BUILDUP, RECON, SKIRMISH, BATTLE, BASE_UNDER_ATTACK, ADVANTAGE, CRISIS, ENDGAME`

音乐根据阵营、科技层、战斗强度和剧情状态分层增加/减少，不简单随机整曲切换。

各阵营保持同一产品音乐语言，同时建立明显音色区别。

## 15. 性能

- 音频更新不进入30Hz权威玩法结果；
- `WarAudioDirector` 可按20Hz或事件驱动更新聚类；
- 远景聚类和虚拟声道不得产生大量主线程分配；
- 音频库按地图/阵营/战役章节流式加载；
- 服务器构建不打入客户端音频资产。

## 16. 资产目录

```text
/Assets/Audio
  /SFX
  /Voice
  /Music
  /Ambience
  /FMOD
/Data/Audio
  audio_registry.json
  voice_registry.json
  music_registry.json
  source_provenance.json
```

## 17. 验收

必须测试：

- 1000单位混战不爆声道；
- 100门炮同时开火仍能听清战略预警；
- 同类炮火聚合/拆分无明显跳变；
- 关键武器盲听识别率>=80%；
- 单位语音连续操作不高频复读；
- 字幕与语音事件一致；
- 失去通信/AI接管等玩法状态有正确音频反馈；
- 低配目标机音频线程无持续性能尖峰；
- 所有生成式资产存在来源与许可记录。
