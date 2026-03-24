using Moq;
using Moq.Protected;
using Sms.Core.Models;
using Sms.Infrastructure.Http;
using Sms.Infrastructure.Http.DTOs;
using Sms.Infrastructure.Http.Responses;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sms.Tests.Infrastructure
{
    [TestFixture]
    public class HttpSmsGatewayTests
    {
        private Mock<HttpMessageHandler> _handlerMock;
        private HttpClient _httpClient;
        private HttpSmsGateway _gateway;

        #region Setup / Teardown

        [SetUp]
        public void SetUp()
        {
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Default);

            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://api.sms.com/")
            };

            _gateway = new HttpSmsGateway(_httpClient);
        }

        [TearDown]
        public void TearDown() => _httpClient.Dispose();

        #endregion

        #region Успешные сценарии (Happy Path)

        [Test]
        public async Task GetMenuAsync_ValidResponse_ReturnsMappedItems()
        {
            // Arrange
            var apiResponse = new GetMenuResponse
            {
                Command = "GetMenu",
                Success = true,
                Data = new GetMenuData
                {
                    MenuItems = new List<MenuItemDto>
                    {
                        new MenuItemDto { Id = "5979224", Name = "Каша гречневая", Price = 50 },
                        new MenuItemDto { Id = "9084246", Name = "Конфеты Коровка", Price = 300 }
                    }
                }
            };
            SetupMockResponse(apiResponse);

            // Act
            var result = (await _gateway.GetMenuAsync()).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Name, Is.EqualTo("Каша гречневая"));
                Assert.That(result[1].Name, Is.EqualTo("Конфеты Коровка"));
            });
        }

        [Test]
        public async Task SendOrderAsync_ValidOrder_CompletesSuccessfully()
        {
            // Arrange
            var order = new Order();
            var apiResponse = new BaseResponse { Command = "SendOrder", Success = true };
            SetupMockResponse(apiResponse);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _gateway.SendOrderAsync(order));
        }

        #endregion

        #region Тесты на невалидные данные и ошибки сервера

        [Test]
        public void GetMenuAsync_ServerReturnsLogicError_ThrowsException()
        {
            // Arrange - Сервер вернул 200 OK, но Success = false
            var apiResponse = new GetMenuResponse
            {
                Success = false,
                ErrorMessage = "Ошибка: неверный формат запроса"
            };
            SetupMockResponse(apiResponse);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _gateway.GetMenuAsync());
            Assert.That(ex.Message, Is.EqualTo("Ошибка: неверный формат запроса"));
        }

        [Test]
        public async Task GetMenuAsync_DataIsNull_ReturnsEmptyCollection()
        {
            // Arrange - Имитируем ситуацию, когда Success: true, но Data пришла как null
            var apiResponse = new GetMenuResponse
            {
                Success = true,
                Data = null!
            };
            SetupMockResponse(apiResponse);

            // Act
            var result = await _gateway.GetMenuAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetMenuAsync_EmptyMenuItems_ReturnsEmptyEnumerable()
        {
            // Arrange - Успешно, но список блюд пуст
            var apiResponse = new GetMenuResponse
            {
                Success = true,
                Data = new GetMenuData { MenuItems = new List<MenuItemDto>() }
            };
            SetupMockResponse(apiResponse);

            // Act
            var result = await _gateway.GetMenuAsync();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SendOrderAsync_EmptyResponse_ThrowsException()
        {
            // Arrange - Сервер вернул 200 OK, но пустое тело (null в JSON)
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
                });

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _gateway.SendOrderAsync(new Order()));
            Assert.That(ex.Message, Is.EqualTo("Empty response content"));
        }

        [Test]
        public void GetMenuAsync_MalformedJson_ThrowsJsonException()
        {
            // Arrange - Вместо JSON пришел мусор
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("<html>Error 200</html>", System.Text.Encoding.UTF8, "application/json")
                });

            // Act & Assert
            Assert.ThrowsAsync<JsonException>(async () => await _gateway.GetMenuAsync());
        }

        #endregion

        #region Валидация структуры исходящих запросов

        [Test]
        public async Task SendOrderAsync_SendsCorrectCommandInJson()
        {
            // Arrange
            var order = new Order();
            SetupMockResponse(new BaseResponse { Success = true });

            // Act
            await _gateway.SendOrderAsync(order);

            // Assert
            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    CheckCommandInRequest(req, "SendOrder")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        #endregion

        #region Helpers (Вспомогательные методы)

        private bool CheckCommandInRequest(HttpRequestMessage request, string expectedCommand)
        {
            if (request.Content == null) return false;

            var content = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            try
            {
                using var jsonDoc = JsonDocument.Parse(content);
                // Поиск свойства Command без учета регистра (т.к. уходит "command")
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    if (string.Equals(property.Name, "Command", StringComparison.OrdinalIgnoreCase))
                    {
                        return property.Value.GetString() == expectedCommand;
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private void SetupMockResponse<T>(T responseContent)
        {
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(responseContent)
                });
        }

        #endregion
    }
}