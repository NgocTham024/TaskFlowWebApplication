# TaskFlow Backend

Backend API cho dự án **TaskFlow** được xây dựng bằng **ASP.NET Core Web API**.

---

## 1. Công nghệ sử dụng

* **ASP.NET Core Web API**
* **C#**
* **Entity Framework Core**
* **MySQL**
* **RESTful API**
* **Swagger / OpenAPI**
* **Git & GitHub**

---

## 2. Clone project

### Clone repository

```bash
git clone https://github.com/NgocTham024/TaskFlowWebApplication.git
```

Di chuyển vào thư mục project:

```bash
cd TaskFlowWebApplication
```

Kiểm tra branch:

```bash
git branch
```

### Clone một branch cụ thể

```bash
git clone -b <branch-name> --single-branch https://github.com/NgocTham024/TaskFlowWebApplication.git
```

Ví dụ:

```bash
git clone -b main --single-branch https://github.com/NgocTham024/TaskFlowWebApplication.git
```

---

## 3. Restore Dependencies

Sau khi clone project, chạy:

```bash
dotnet restore
```

Lệnh này sẽ tải các **NuGet packages** cần thiết cho project.

Kiểm tra project:

```bash
dotnet build
```

Nếu build thành công, project đã được cài đặt dependencies đầy đủ.

---

## 4. Chạy Backend

Chạy project:

```bash
dotnet run
```

Hoặc sử dụng:

```bash
dotnet watch run
```

`dotnet watch run` sẽ tự động reload project khi source code thay đổi.

Sau khi chạy thành công, terminal sẽ hiển thị địa chỉ API, ví dụ:

```text
http://localhost:x000
```

> Port thực tế phụ thuộc vào cấu hình của project.

---

## 5. Swagger API

Project sử dụng **Swagger / OpenAPI** để xem và kiểm tra các API.

Sau khi chạy Backend, truy cập:

```text
http://localhost:<port>/swagger
```

Ví dụ:

```text
http://localhost:5000/swagger
```

Swagger cho phép:

* Xem danh sách API
* Xem request/response
* Test API trực tiếp
* Kiểm tra HTTP methods
* Kiểm tra các endpoint

---

## 6. Các API chính

Các API được tổ chức theo từng Controller.

| Method | Endpoint          | Chức năng        |
| ------ | ----------------- | ---------------- |
| GET    | `/api/tasks/{id}` | Lấy task theo ID |
| POST   | `/api/tasks`      | Tạo task         |
| PUT    | `/api/tasks/{id}` | Cập nhật task    |
| DELETE | `/api/tasks/{id}` | Xóa task         |

---

## 7. Quy trình làm việc với Git

### Kiểm tra branch hiện tại

```bash
git branch
```

### Lấy code mới nhất

```bash
git pull origin <branch-name>
```

Ví dụ:

```bash
git pull origin main
```

---

## 8. Tạo branch mới

Không nên code trực tiếp trên `main`.

Tạo branch mới:

```bash
git checkout -b feature/<ten-feature>
```

Ví dụ:

```bash
git checkout -b feature/task-api
```

Hoặc:

```bash
git switch -c feature/task-api
```

---

## 9. Commit code

Kiểm tra các file đã thay đổi:

```bash
git status
```

Add code:

```bash
git add .
```

Commit:

```bash
git commit -m "feat: add task API"
```

---

## 10. Push branch lên GitHub

```bash
git push -u origin feature/task-api
```

Sau khi push thành công, tạo **Pull Request** trên GitHub.

---

## 11. Quy ước Commit

Khuyến khích sử dụng **Conventional Commits**:

| Prefix      | Ý nghĩa                    |
| ----------- | -------------------------- |
| `feat:`     | Thêm chức năng mới         |
| `fix:`      | Sửa lỗi                    |
| `refactor:` | Cải thiện cấu trúc code    |
| `docs:`     | Cập nhật tài liệu          |
| `test:`     | Thêm hoặc sửa test         |
| `chore:`    | Công việc cấu hình/project |

Ví dụ:

```bash
git commit -m "feat: add task controller"
```

---

## 12. Cập nhật code trước khi làm việc

Trước khi bắt đầu làm việc:

```bash
git checkout main
```

Lấy code mới nhất:

```bash
git pull origin main
```

Tạo branch mới:

```bash
git checkout -b feature/<ten-feature>
```

---

## 13. Lưu ý quan trọng

### Không commit thông tin bảo mật

Không đưa các thông tin sau lên GitHub:

```text
Database password
JWT Secret
API Key
Connection String chứa password
Secret Key
```

Không nên viết trực tiếp password hoặc secret vào source code.

### Không commit thư mục build

Các thư mục sau không cần push lên GitHub:

```text
bin/
obj/
```

Đảm bảo `.gitignore` có các thư mục này.

---

## 14. Kiểm tra project trước khi Pull Request

Trước khi tạo Pull Request, kiểm tra project.

### Restore

```bash
dotnet restore
```

### Build

```bash
dotnet build
```

### Test

Nếu project có Unit Test:

```bash
dotnet test
```

### Chạy project

```bash
dotnet run
```

### Kiểm tra Swagger

Mở:

```text
/swagger
```

và test các API liên quan.
