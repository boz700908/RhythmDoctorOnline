# 定时广播同步 API 文档

## 概述

为了解决网络丢包导致的客户端状态不同步问题，服务器实现了定时广播机制。服务器会定期向大厅和房间内的玩家广播完整的状态信息，确保客户端能够及时同步最新状态。

## 配置说明

### 广播间隔配置

在 `src/config/constants.ts` 中配置：

```typescript
export const BROADCAST_INTERVAL = {
  LOBBY: 5000,  // 大厅广播间隔（毫秒），默认5秒
  ROOM: 5000    // 房间广播间隔（毫秒），默认5秒
};

export const BROADCAST_ENABLED = {
  LOBBY: true,  // 是否启用大厅定时广播
  ROOM: true    // 是否启用房间定时广播
};
```

### 启动方式

定时广播服务在服务器启动时自动启动，无需手动调用。

---

## 一、大厅定时广播

### 消息类型

`lobby/sync`

### 广播频率

每 5 秒（可配置）

### 广播对象

大厅内的所有玩家

### 消息格式

```json
{
  "type": "lobby/sync",
  "data": {
    "players": [
      {
        "userId": 1,
        "username": "玩家名称",
        "avatar": "头像URL或null"
      }
    ],
    "rooms": [
      {
        "id": "room_1",
        "name": "房间名称",
        "maxPlayers": 8,
        "hasPassword": false,
        "chartUrl": "谱面URL",
        "chartName": "谱面名称",
        "ownerId": 1,
        "status": "waiting",
        "playerCount": 3
      }
    ]
  }
}
```

### 字段说明

#### data.players（大厅玩家列表）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| userId | number | 玩家用户ID |
| username | string | 玩家用户名 |
| avatar | string \| null | 玩家头像URL，可能为null |

#### data.rooms（房间列表）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| id | string | 房间ID，格式为 "room_数字" |
| name | string | 房间名称 |
| maxPlayers | number | 房间最大玩家数（1-8） |
| hasPassword | boolean | 是否有密码保护 |
| chartUrl | string | 谱面文件URL |
| chartName | string | 谱面名称 |
| ownerId | number | 房主的用户ID |
| status | string | 房间状态："waiting"（等待中）或 "playing"（游戏中） |
| playerCount | number | 当前房间内的玩家数量 |

### 使用场景

1. **初始同步**：客户端加入大厅后，会在5秒内收到第一次完整的状态同步
2. **丢包恢复**：如果客户端因网络问题丢失了某些实时事件（如 `room/created`、`room/updated`），定时广播可以确保客户端在5秒内恢复正确状态
3. **状态校验**：客户端可以使用定时广播的数据校验本地状态是否正确

### 客户端处理建议

```typescript
// 客户端示例代码
socket.on('message', (message) => {
  const data = JSON.parse(message);

  if (data.type === 'lobby/sync') {
    // 完全替换本地的大厅玩家列表和房间列表
    updateLobbyPlayers(data.data.players);
    updateRoomList(data.data.rooms);
  }
});
```

---

## 二、房间定时广播

### 消息类型

`room/sync`

### 广播频率

每 5 秒（可配置）

### 广播对象

每个房间内的所有玩家（每个房间独立广播）

### 消息格式

```json
{
  "type": "room/sync",
  "data": {
    "room": {
      "id": "room_1",
      "name": "房间名称",
      "maxPlayers": 8,
      "hasPassword": false,
      "chartUrl": "https://example.com/chart.zip",
      "chartName": "谱面名称",
      "ownerId": 1,
      "status": "waiting"
    },
    "players": [
      {
        "userId": 1,
        "username": "玩家名称",
        "avatar": "头像URL或null",
        "ready": true
      }
    ]
  }
}
```

### 字段说明

#### data.room（房间信息）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| id | string | 房间ID，格式为 "room_数字" |
| name | string | 房间名称 |
| maxPlayers | number | 房间最大玩家数（1-8） |
| hasPassword | boolean | 是否有密码保护 |
| chartUrl | string | 谱面文件URL |
| chartName | string | 谱面名称 |
| ownerId | number | 房主的用户ID |
| status | string | 房间状态："waiting"（等待中）或 "playing"（游戏中） |

#### data.players（房间内玩家列表）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| userId | number | 玩家用户ID |
| username | string | 玩家用户名 |
| avatar | string \| null | 玩家头像URL，可能为null |
| ready | boolean | 玩家是否已准备 |

