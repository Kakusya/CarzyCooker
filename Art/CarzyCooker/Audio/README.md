# 经营短音

本项目原创合成，不使用录音、外部采样或第三方音乐。可编辑源为`export_feedback.py`，只需Python标准库；在仓库根运行`python Art/CarzyCooker/Audio/export_feedback.py`，写入原Sound资源路径`Unity/Assets/Res/Sound/Product/`。正常导入及资源集合刷新仍由Unity完成。

| 声音 | 秒 | 用途 |
|---|---:|---|
| Pickup | 0.11 | 本人实际拿取 |
| Place | 0.12 | 放置、入容器、分装 |
| WorkStart | 0.18 | 实际开始加工 |
| WorkFinished | 0.34 | 当前餐厅实际完工 |
| Serve | 0.42 | 本人实际交付 |
| LevelComplete | 0.72 | 本关自然完成 |

文件均为44.1kHz、单声道16bit PCM，峰值不超过0.52；非循环、非空间化，分组`Sound.Default`。参数可以在源脚本中调整后重导出。实际GF播放、触发和生命周期验收已通过；2026-10-20用户听取样板六段短音后明确回复“已试听，可以接受”。详细证据见`openspec/changes/archive/2026-10-20-restaurant-feedback-audio-pass/execution-notes.md`；没有据此宣称完成专业混音或多设备校准。
