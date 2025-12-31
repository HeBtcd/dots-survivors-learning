# Unity 编辑器 Play 模式鼠标移动卡顿

## 复现

### 环境

- Unity：`6000.0.58f2`
- Input System：`com.unity.inputsystem@1.14.2`

### 具体表现

- 只在 **Editor + Play 模式** 出现；Build/Standalone 明显更正常
- 只要鼠标在 **Play 模式** 移动就立刻卡，不动鼠标就恢复
- Profiler（Editor 模式）热点在 **EditorLoop**，而不是 PlayerLoop
- Console 没有持续输出（不是 Log/Warning/Error 刷屏导致）

## 根本原因
Unity 编辑器在处理高回报率鼠标输入事件时，Windows 消息队列处理不过来，导致 EditorLoop 在每一帧处理大量输入消息，从而拖慢了主线程。

## Issue Tracker

- [Issue 1336740：Editor slows down when rapidly moving mouse with high polling rate in play mode](https://issuetracker.unity3d.com/issues/editor-slows-down-when-rapidly-moving-mouse-with-high-polling-rate-in-play-mode)

- [Issue UUM-1484：High polling rate mice are causing performance issues (windows, editor)](https://issuetracker.unity3d.com/issues/high-polling-rate-mice-are-causing-performance-issues)
