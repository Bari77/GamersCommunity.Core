using GamersCommunity.Core.Exceptions;
using Newtonsoft.Json;

namespace GamersCommunity.Core.Rabbit
{
    public static class ConsumerParamParser
    {
        public static short ToShort(string data)
        {
            if (!short.TryParse(data, out short result))
            {
                throw new BadRequestException("Can't be parse to short");
            }
            return result;
        }

        public static int ToInt(string data)
        {
            if (!int.TryParse(data, out int result))
            {
                throw new BadRequestException("Can't be parse to int");
            }
            return result;
        }

        public static int ToInt(int? data)
        {
            if (!int.TryParse(data.ToString(), out int result))
            {
                throw new BadRequestException("Can't be parse to int");
            }
            return result;
        }

        public static long ToLong(string data)
        {
            if (!long.TryParse(data, out long result))
            {
                throw new BadRequestException("Can't be parse to long");
            }
            return result;
        }

        public static T ToObject<T>(string data)
        {
            T? result;
            try
            {
                result = JsonConvert.DeserializeObject<T>(data);
            }
            catch (Exception)
            {
                throw new BadRequestException("Param can't be parse");
            }

            if (result == null)
            {
                throw new BadRequestException("Param can't be null");
            }

            return result;
        }

        public static T? ToNullableObject<T>(string data)
        {
            T? result;
            try
            {
                result = JsonConvert.DeserializeObject<T>(data);
            }
            catch (Exception)
            {
                throw new BadRequestException("Param can't be parse");
            }

            return result;
        }

        public static List<T> ToListObject<T>(string data, bool throwIfEmpty = true)
        {
            List<T>? result;
            try
            {
                result = JsonConvert.DeserializeObject<List<T>>(data);
            }
            catch (Exception)
            {
                throw new BadRequestException("Param can't be parse");
            }

            if (result == null)
            {
                throw new BadRequestException("Param can't be null");
            }

            if (throwIfEmpty && result.Count == 0)
            {
                throw new BadRequestException("Param list can't be empty");
            }

            return result;
        }

        public static List<T>? ToNullableListObject<T>(string data)
        {
            try
            {
                return JsonConvert.DeserializeObject<List<T>>(data);
            }
            catch (Exception)
            {
                throw new BadRequestException("Param can't be parse");
            }
        }
    }
}
