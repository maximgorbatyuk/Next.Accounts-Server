using System;
using System.Runtime.InteropServices.ComTypes;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Web_Space
{
    public interface IJsonParser
    {
        /// <summary>
        /// Парсит строку в Json-формате в объект
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">Строковое отображение объекта</param>
        /// <returns>Либо объект, либо default(T)</returns>
        T Parse<T>(string source);

        /// <summary>
        /// Возвращает строковое отобращение объекта
        /// </summary>
        /// <typeparam name="T">Исходный объект</typeparam>
        /// <param name="obj"></param>
        /// <returns>Строковое Json-отображение объекта</returns>
        string ToJson<T>(T obj);
    }
}