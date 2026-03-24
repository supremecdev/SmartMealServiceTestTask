using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Moq;
using Sms.Infrastructure.gRPC;
using Sms.Test;
using System.Linq.Expressions;

namespace Sms.Tests.Infrastructure
{
    [TestFixture]
    public class GrpcSmsGatewayTests
    {
        private Mock<SmsTestService.SmsTestServiceClient> _mockClient;
        private GrpcSmsGateway _gateway;

        #region Setup

        [SetUp]
        public void SetUp()
        {
            _mockClient = new Mock<SmsTestService.SmsTestServiceClient>();
            _gateway = new GrpcSmsGateway(_mockClient.Object);
        }

        #endregion

        #region Успешные сценарии (Happy Path)

        [Test]
        public async Task GetMenuAsync_ValidResponse_ReturnsMappedItems()
        {
            // Arrange
            var response = new GetMenuResponse { Success = true };
            response.MenuItems.Add(new Sms.Test.MenuItem { Id = "5979224", Name = "Каша гречневая", Price = 50.0 });
            response.MenuItems.Add(new Sms.Test.MenuItem { Id = "9084246", Name = "Конфеты Коровка", Price = 300.0 });

            SetupRpcCall<GetMenuResponse>(
                x => x.GetMenuAsync(It.IsAny<BoolValue>(), null, null, default),
                response);

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
            var order = new Sms.Core.Models.Order();
            var response = new SendOrderResponse { Success = true };

            SetupRpcCall<SendOrderResponse>(
                x => x.SendOrderAsync(It.IsAny<Sms.Test.Order>(), null, null, default),
                response);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _gateway.SendOrderAsync(order));
        }

        #endregion

        #region Тесты на невалидные данные и ошибки сервера

        [Test]
        public void GetMenuAsync_ServerReturnsLogicError_ThrowsException()
        {
            // Arrange - Эквивалент Success = false в JSON
            var response = new GetMenuResponse
            {
                Success = false,
                ErrorMessage = "Ошибка: неверный формат запроса"
            };

            SetupRpcCall<GetMenuResponse>(
                x => x.GetMenuAsync(It.IsAny<BoolValue>(), null, null, default),
                response);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _gateway.GetMenuAsync());
            Assert.That(ex.Message, Is.EqualTo("Ошибка: неверный формат запроса"));
        }

        [Test]
        public async Task GetMenuAsync_EmptyMenuItems_ReturnsEmptyEnumerable()
        {
            // Arrange - Успешно, но список MenuItems пуст (в gRPC repeated поле не бывает null, оно просто пустое)
            var response = new GetMenuResponse { Success = true };

            SetupRpcCall<GetMenuResponse>(
                x => x.GetMenuAsync(It.IsAny<BoolValue>(), null, null, default),
                response);

            // Act
            var result = await _gateway.GetMenuAsync();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetMenuAsync_RpcException_ThrowsException()
        {
            // Arrange - Эквивалент MalformedJson или ошибки сети (уровень протокола gRPC)
            var rpcException = new RpcException(new Status(StatusCode.Internal, "Internal Server Error"));

            _mockClient
                .Setup(x => x.GetMenuAsync(It.IsAny<BoolValue>(), null, null, default))
                .Throws(rpcException);

            // Act & Assert
            Assert.ThrowsAsync<RpcException>(async () => await _gateway.GetMenuAsync());
        }

        #endregion

        #region Валидация структуры исходящих запросов

        [Test]
        public async Task SendOrderAsync_SendsCorrectData()
        {
            // Arrange
            var order = new Sms.Core.Models.Order();
            // Допустим, мы хотим проверить, что ID заказа передается верно
            var expectedId = order.Id.ToString();

            SetupRpcCall<SendOrderResponse>(
                x => x.SendOrderAsync(It.IsAny<Sms.Test.Order>(), null, null, default),
                new SendOrderResponse { Success = true });

            // Act
            await _gateway.SendOrderAsync(order);

            // Assert
            // Проверяем, что клиент вызвал метод с объектом Order, у которого правильный Id
            _mockClient.Verify(x => x.SendOrderAsync(
                It.Is<Sms.Test.Order>(o => o.Id == expectedId),
                null, null, default),
                Times.Once);
        }

        #endregion

        #region Helpers

        private void SetupRpcCall<TResponse>(
            Expression<Func<SmsTestService.SmsTestServiceClient, AsyncUnaryCall<TResponse>>> expression,
            TResponse response)
        {
            var call = new AsyncUnaryCall<TResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

            _mockClient.Setup(expression).Returns(call);
        }

        #endregion
    }
}