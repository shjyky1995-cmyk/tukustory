namespace Tuku.UnitTests.Contracts
{
    using Newtonsoft.Json;
    using Tuku.Contracts.Cad;
    using Xunit;

    public class CadProtocolTests
    {
        private static T MustDeserialize<T>(string json)
        {
            var value = JsonConvert.DeserializeObject<T>(json);
            if (value == null)
            {
                throw new System.InvalidOperationException("反序列化结果为空");
            }

            return value;
        }

        [Fact]
        public void Envelope_RoundTrips_WithChineseAndSpecialCharacters()
        {
            var request = new CadInsertPracticeRequest
            {
                TargetInstance = "acad-1234",
                TargetDocumentId = "doc-1",
                PracticeId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
                PracticeRevision = 3,
                Items = new System.Collections.Generic.List<CadTextItem>
                {
                    new CadTextItem { Kind = CadTextItemKind.Name, Text = "楼地面做法 ①", Order = 1 },
                    new CadTextItem { Kind = CadTextItemKind.Code, Text = "D-01", Order = 2 },
                    new CadTextItem { Kind = CadTextItemKind.Layer, Text = "20 厚 1:2 水泥砂浆\n\\P 第二行", Order = 3 }
                },
                Style = new CadInsertStyle
                {
                    TextStyleName = "standard",
                    TextHeight = 2.5,
                    ContentWidth = 40,
                    ParagraphSpacing = 1.2,
                    LayerName = "TUKU-做法"
                }
            };

            var message = ProtocolMessage.Create(CadProtocol.MessageInsertPractice, "req-1", request);
            var json = JsonConvert.SerializeObject(message);
            var restored = MustDeserialize<ProtocolMessage>(json);

            Assert.Equal(CadProtocol.CurrentVersion, restored.ProtocolVersion);
            Assert.Equal("req-1", restored.RequestId);
            Assert.Equal(CadProtocol.MessageInsertPractice, restored.Type);
            var payload = restored.ReadPayload<CadInsertPracticeRequest>();
            if (payload == null)
            {
                Assert.Fail("payload 反序列化不应为空");
                return;
            }

            Assert.Equal("楼地面做法 ①", payload.Items[0].Text);
            Assert.Equal("20 厚 1:2 水泥砂浆\n\\P 第二行", payload.Items[2].Text);
            Assert.Equal(3, payload.Items.Count);
            Assert.Equal(2.5, payload.Style.TextHeight);
        }

        [Fact]
        public void ProtocolVersion_Boundaries_AreChecked()
        {
            Assert.True(CadProtocol.IsSupportedVersion(1));
            Assert.False(CadProtocol.IsSupportedVersion(0));
            Assert.False(CadProtocol.IsSupportedVersion(2));
        }

        [Fact]
        public void EmptyPayload_ReadsAsDefault()
        {
            var message = ProtocolMessage.CreateEmpty(CadProtocol.MessageGetRequestStatus, "req-2");
            var json = JsonConvert.SerializeObject(message);
            var restored = MustDeserialize<ProtocolMessage>(json);
            var payload = restored.ReadPayload<CadGetRequestStatus>();
            if (payload == null)
            {
                Assert.Fail("payload 反序列化不应为空");
                return;
            }

            Assert.Null(payload.RequestId);
        }

        [Fact]
        public void StatusMessage_RoundTrips_AllPhases()
        {
            foreach (var phase in new[]
            {
                CadInsertPhase.Received,
                CadInsertPhase.WaitingForPoint,
                CadInsertPhase.Executing,
                CadInsertPhase.Succeeded,
                CadInsertPhase.Cancelled,
                CadInsertPhase.Failed,
                CadInsertPhase.Unknown
            })
            {
                var status = new CadInsertStatus { RequestId = "r", Phase = phase, Message = "消息", CreatedObjectCount = 4 };
                var json = JsonConvert.SerializeObject(status);
                var restored = MustDeserialize<CadInsertStatus>(json);
                Assert.Equal(phase, restored.Phase);
                Assert.Equal(4, restored.CreatedObjectCount);
            }
        }
    }
}
