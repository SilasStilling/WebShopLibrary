using Moq;
using System;
using System.Data;
using System.Threading.Tasks;
using WebShopLibrary;
using WebShopLibrary.Database;
using Xunit;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace WebShopLibrary.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<DBConnection> _mockDbConnection; // Fix for Problem 1
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockDbConnection = new Mock<DBConnection>(); // Fix for Problem 1
            _authService = new AuthService(_mockDbConnection.Object);
        }

        [Fact]
        public async Task RegisterUser_ShouldRegisterSuccessfully()
        {
            // Arrange
            var username = "testuser";
            var password = "Hut!FYys76gjyedgy";
            var role = "user";

            _mockDbConnection.Setup(db => db.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<SqlParameter[]>()))
                .ReturnsAsync(new DataTable());

            _mockDbConnection.Setup(db => db.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<SqlParameter[]>()))
                .ReturnsAsync(1);

            // Act
            var result = await _authService.RegisterUser(username, password, role);

            // Assert
            Xunit.Assert.True(result);
        }

        [Fact]
        public async Task RegisterUser_ShouldThrowException_WhenUsernameTaken()
        {
            // Arrange
            var username = "testuser";
            var password = "Hut!FYys76gjyedgy";
            var role = "user";

            var dataTable = new DataTable();
            dataTable.Columns.Add("Count");
            dataTable.Rows.Add(1);

            _mockDbConnection.Setup(db => db.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<SqlParameter[]>()))
                .ReturnsAsync(dataTable);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<Exception>(() => _authService.RegisterUser(username, password, role));
        }

        [Fact]
        public async Task Login_ShouldLoginSuccessfully()
        {
            // Arrange
            var username = "testuser";
            var password = "Hut!FYys76gjyedgy";
            var role = "user";
            var hashedPassword = _authService.HashPassword(password);

            var dataTable = new DataTable();
            dataTable.Columns.Add("Id");
            dataTable.Columns.Add("Username");
            dataTable.Columns.Add("PasswordHash");
            dataTable.Columns.Add("Role");
            dataTable.Rows.Add(1, username, hashedPassword, role);

            _mockDbConnection.Setup(db => db.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<SqlParameter[]>()))
                .ReturnsAsync(dataTable);

            // Act
            var user = await _authService.Login(username, password);

            // Assert
            Xunit.Assert.NotNull(user);
            Xunit.Assert.Equal(username, user.Username);
            Xunit.Assert.Equal(role, user.Role);
        }

        [Fact]
        public async Task Login_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var username = "testuser";
            var password = "Hut!FYys76gjyedgy";

            var dataTable = new DataTable();

            _mockDbConnection.Setup(db => db.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<SqlParameter[]>()))
                .ReturnsAsync(dataTable);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<Exception>(() => _authService.Login(username, password));
        }

        [Fact]
        public async Task Login_ShouldThrowException_WhenPasswordIncorrect()
        {
            // Arrange
            var username = "testuser";
            var password = "Hut!FYys76gjyedgy";
            var role = "user";
            var hashedPassword = _authService.HashPassword("wrongpassword");

            var dataTable = new DataTable();
            dataTable.Columns.Add("Id");
            dataTable.Columns.Add("Username");
            dataTable.Columns.Add("PasswordHash");
            dataTable.Columns.Add("Role");
            dataTable.Rows.Add(1, username, hashedPassword, role);

            _mockDbConnection.Setup(db => db.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<SqlParameter[]>()))
                .ReturnsAsync(dataTable);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<Exception>(() => _authService.Login(username, password));
        }
    }
}