# 铺面上传接口文档

## 基本信息

| 项目 | 说明 |
|------|------|
| **接口路径** | `/upload_chart` |
| **请求方法** | `POST` |
| **内容类型** | `multipart/form-data` |

---

## 请求参数

| 参数名 | 类型 | 必需 | 说明 |
|--------|------|------|------|
| `file` | File | 是 | 上传的铺面文件，支持 `.zip` 或 `.rdzip` 格式 |

**文件限制：**
- 允许格式：`.zip`、`.rdzip`
- 最大大小：250MB

---

## 成功响应 (200)

```json
{
  "success": true,
  "message": "文件上传成功",
  "data": {
    "url": "http://example.com/charts/1704067200000_a1b2c3d4e5f6g7h8.zip",
    "fileName": "1704067200000_a1b2c3d4e5f6g7h8.zip",
    "originalName": "my_chart.zip",
    "size": 5242880,
    "extension": ".zip"
  }
}
```

**响应字段说明：**
- `url` - 文件的完整访问URL
- `fileName` - 服务器生成的唯一文件名
- `originalName` - 原始上传的文件名
- `size` - 文件大小（字节）
- `extension` - 文件扩展名

---

## 错误响应

### 1. 没有上传文件 (400)
```json
{
  "success": false,
  "message": "没有上传文件"
}
```

### 2. 文件格式不支持 (400)
```json
{
  "success": false,
  "message": "只允许上传 .zip 或 .rdzip 文件"
}
```

### 3. 文件大小超限 (400)
```json
{
  "success": false,
  "message": "文件大小超过 250MB 限制"
}
```

### 4. 服务器错误 (500)
```json
{
  "success": false,
  "message": "上传失败"
}
```

---

## 使用示例

### cURL
```bash
curl -X POST http://localhost:3000/upload_chart \
  -F "file=@/path/to/chart.zip"
```

### JavaScript (Fetch API)
```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);

const response = await fetch('http://localhost:3000/upload_chart', {
  method: 'POST',
  body: formData
});

const result = await response.json();
console.log(result);
```

### JavaScript (Axios)
```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);

const response = await axios.post('http://localhost:3000/upload_chart', formData, {
  headers: {
    'Content-Type': 'multipart/form-data'
  }
});

console.log(response.data);
```

---

## 技术细节

- **文件命名规则**：`时间戳_随机字符串.扩展名`
  - 例如：`1704067200000_a1b2c3d4e5f6g7h8.zip`
  - 确保文件名唯一，避免覆盖

- **存储位置**：`public/charts/` 目录

- **访问方式**：上传成功后，可通过返回的 `url` 直接访问文件
