# 🏋️‍♂️ Gym Management System

A web-based application built with **ASP.NET Core MVC** using **Clean Architecture** to manage gym operations such as members, attendance, and payments.

---

## 📖 About the Project
This system helps gym administrators, trainers, and members to efficiently manage their daily operations.  
It provides role-based access to ensure each user only sees what’s relevant to them.

---

## 🚀 Features
- 👥 **Members Management** – Add, edit, and manage gym members.
- 📅 **Attendance Tracking** – Track member check-ins and check-outs.
- 💳 **Payments Management** – Handle membership payments and renewals.
- 🔐 **Authentication & Authorization** – Secure login with role-based access (Admin, Trainer, Member).
- 📊 **Dashboard** – Overview of gym statistics.

---

## 🛠️ Tech Stack
- **ASP.NET Core MVC**  
- **Entity Framework Core**  
- **Identity for Authentication & Authorization**  
- **SQL Server**  
- **Bootstrap 5** for UI  

---

## ⚙️ Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/NadaAshraf12/GymManagementSystem.git

2. Navigate to the project folder:

   ```bash
   cd GymManagementSystem
   ```

3. Update the database (apply migrations):

   ```bash
   dotnet ef database update
   ```

4. Run the project:

   ```bash
   dotnet run
   ```

---

## 💻 Usage

* **Admin** can manage members, payments, and attendance.
* **Trainers** can track attendance.
* **Members** can view their attendance and payment history.

---

## 🤝 Contributing

Contributions are welcome!
Feel free to fork the repo and submit a pull request.

---
