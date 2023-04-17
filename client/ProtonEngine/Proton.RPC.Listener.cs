using LiteNetLib;
using Proton.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Proton
{
    public static class RpcListener
    {
        private static Dictionary<int, List<RpcCallback>> rpcListeners = new Dictionary<int, List<RpcCallback>>();
        public delegate void RpcCallback(List<NetworkValue> arguments, DeliveryMethod deliveryMethod);

        /// <summary>
        /// Добавляет функцию в список слушателей некоторого RPC по его ID.
        /// </summary>
        public static void AddCallback(int rpcId, RpcCallback callback)
        {
            if (!rpcListeners.ContainsKey(rpcId))
            {
                rpcListeners[rpcId] = new List<RpcCallback>();
            }

            if (rpcListeners[rpcId].Contains(callback))
            {
                return;
            }

            rpcListeners[rpcId].Add(callback);
        }

        /// <summary>
        /// Добавляет функцию в список слушателей некоторого RPC по его имени.
        /// </summary>
        public static void AddCallback(string callbackName, RpcCallback callback)
        {
            AddCallback(GetStringHash(callbackName), callback);
        }

        /// <summary>
        /// Удаляет функцию из списка слушателей некоторого RPC по его ID.
        /// </summary>
        public static void RemoveListener(int rpcId, RpcCallback callback)
        {
            if (rpcListeners.ContainsKey(rpcId))
            {
                if (rpcListeners[rpcId].Contains(callback))
                {
                    rpcListeners[rpcId].Remove(callback);
                }
            }
        }

        /// <summary>
        /// Удаляет функцию из списка слушателей некоторого RPC по его имени.
        /// </summary>
        public static void RemoveListener(string callbackName, RpcCallback callback)
        {
            RemoveListener(GetStringHash(callbackName), callback);
        }

        /// <summary>
        /// Вызывает RPC функции по ID.
        /// </summary>
        public static void Invoke(int rpcId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            if (rpcListeners.ContainsKey(rpcId))
            {
                foreach (RpcCallback callback in rpcListeners[rpcId])
                {
                    callback(arguments, deliveryMethod);
                }
            }
        }

        /// <summary>
        /// Получает хэш строки.
        /// </summary>
        /// <returns>Хэш строки.</returns>
        public static int GetStringHash(string value)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    hash = hash * 31 * i * 3 + c.GetHashCode();
                }
                return hash;
            }
        }
    }
}