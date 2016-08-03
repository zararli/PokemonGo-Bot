using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Exceptions;
using POGOProtos.Networking.Envelopes;
using PokemonGo.RocketAPI.Helpers;

namespace PokemonGo.RocketAPI.Extensions
{
    public static class HttpClientExtensions
    {
        static int error = 0;

        public static async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(this System.Net.Http.HttpClient client,
            string url, RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            Debug.WriteLine($"Requesting {typeof(TResponsePayload).Name}");
            var response = await PostProto<TRequest>(client, url, requestEnvelope);


            while (response.Returns.Count == 0)
            {
                if (error >= 3)
                {
                    error = 0;
                    throw new InvalidResponseException();
                }
                await RandomHelper.RandomDelay(150, 300);
                response = await PostProto<TRequest>(client, url, requestEnvelope);
                if (response.Returns.Count == 0)
                {
                    error++;
                    Logger.Error($"Error at Request PostProtoPayload {typeof(TResponsePayload).Name} retrying {error}/3");
                } else
                {
                    error = 0;
                    break;
                }
            } 

            //Decode payload
            //todo: multi-payload support
            var payload = response.Returns[0];
            var parsedPayload = new TResponsePayload();
            parsedPayload.MergeFrom(payload);

            return parsedPayload;
        }

        public static async Task<ResponseEnvelope> PostProto<TRequest>(this System.Net.Http.HttpClient client, string url,
            RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            //Encode payload and put in envelop, then send
            var data = requestEnvelope.ToByteString();
            var result = await client.PostAsync(url, new ByteArrayContent(data.ToByteArray()));

            //Decode message
            var responseData = await result.Content.ReadAsByteArrayAsync();
            var codedStream = new CodedInputStream(responseData);
            var decodedResponse = new ResponseEnvelope();
            decodedResponse.MergeFrom(codedStream);

            return decodedResponse;
        }
    }
}