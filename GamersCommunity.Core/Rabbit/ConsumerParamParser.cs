using GamersCommunity.Core.Exceptions;
using Newtonsoft.Json;

namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// Helper methods to parse RabbitMQ payload fields into strongly-typed values.
    /// </summary>
    /// <remarks>
    /// All methods throw <see cref="BadRequestException"/> when the provided input
    /// cannot be parsed into the requested type (or violates simple validation rules).
    /// JSON conversion relies on <see cref="JsonConvert"/> from Newtonsoft.Json.
    /// </remarks>
    public static class ConsumerParamParser
    {
        /// <summary>
        /// Parses a <see cref="string"/> to <see cref="short"/>.
        /// </summary>
        /// <param name="data">Source string to parse.</param>
        /// <returns>The parsed <see cref="short"/> value.</returns>
        /// <exception cref="BadRequestException">Thrown when parsing fails.</exception>
        public static short ToShort(string data)
        {
            if (!short.TryParse(data, out short result))
            {
                throw new BadRequestException("Can't be parse to short");
            }
            return result;
        }

        /// <summary>
        /// Parses a <see cref="string"/> to <see cref="int"/>.
        /// </summary>
        /// <param name="data">Source string to parse.</param>
        /// <returns>The parsed <see cref="int"/> value.</returns>
        /// <exception cref="BadRequestException">Thrown when parsing fails.</exception>
        public static int ToInt(string data)
        {
            if (!int.TryParse(data, out int result))
            {
                throw new BadRequestException("Can't be parse to int");
            }
            return result;
        }

        /// <summary>
        /// Parses a nullable <see cref="int"/> to a non-null <see cref="int"/>.
        /// </summary>
        /// <param name="data">Nullable integer value.</param>
        /// <returns>The non-null <see cref="int"/> value.</returns>
        /// <exception cref="BadRequestException">
        /// Thrown when the input is null or cannot be parsed to <see cref="int"/>.
        /// </exception>
        public static int ToInt(int? data)
        {
            if (!int.TryParse(data.ToString(), out int result))
            {
                throw new BadRequestException("Can't be parse to int");
            }
            return result;
        }

        /// <summary>
        /// Parses a <see cref="string"/> to <see cref="long"/>.
        /// </summary>
        /// <param name="data">Source string to parse.</param>
        /// <returns>The parsed <see cref="long"/> value.</returns>
        /// <exception cref="BadRequestException">Thrown when parsing fails.</exception>
        public static long ToLong(string data)
        {
            if (!long.TryParse(data, out long result))
            {
                throw new BadRequestException("Can't be parse to long");
            }
            return result;
        }

        /// <summary>
        /// Deserializes a JSON string into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target CLR type.</typeparam>
        /// <param name="data">JSON payload as a string.</param>
        /// <returns>An instance of <typeparamref name="T"/>.</returns>
        /// <exception cref="BadRequestException">
        /// Thrown when the JSON cannot be deserialized or the result is <see langword="null"/>.
        /// </exception>
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

        /// <summary>
        /// Deserializes a JSON string into a nullable object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target CLR type.</typeparam>
        /// <param name="data">JSON payload as a string.</param>
        /// <returns>
        /// An instance of <typeparamref name="T"/> or <see langword="null"/> if the JSON
        /// represents a null value.
        /// </returns>
        /// <exception cref="BadRequestException">Thrown when the JSON cannot be deserialized.</exception>
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

        /// <summary>
        /// Deserializes a JSON array into a <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="data">JSON array as a string.</param>
        /// <param name="throwIfEmpty">
        /// When <see langword="true"/>, throws if the resulting list is empty.
        /// Defaults to <see langword="true"/>.
        /// </param>
        /// <returns>A non-null list of <typeparamref name="T"/>.</returns>
        /// <exception cref="BadRequestException">
        /// Thrown when the JSON cannot be deserialized, the result is null, or
        /// <paramref name="throwIfEmpty"/> is <see langword="true"/> and the list is empty.
        /// </exception>
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

        /// <summary>
        /// Deserializes a JSON array into a nullable <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="data">JSON array as a string.</param>
        /// <returns>
        /// A <see cref="List{T}"/> instance, or <see langword="null"/> if the JSON represents a null value.
        /// </returns>
        /// <exception cref="BadRequestException">Thrown when the JSON cannot be deserialized.</exception>
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
