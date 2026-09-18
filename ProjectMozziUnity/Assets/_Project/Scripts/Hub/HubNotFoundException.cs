using System;
using System.Collections.Generic;
using System.Text;

namespace Mozzi.Hubs
{
    // 등록되지 않은 타입을 Get 으로 요구했을 때 던지는 예외다.
    public sealed class HubNotFoundException : Exception
    {
        public HubNotFoundException(Type wanted, string hubName, IReadOnlyCollection<Type> registered)
            : base(BuildMessage(wanted, hubName, registered))
        {
        }

        private static string BuildMessage(Type wanted, string hubName, IReadOnlyCollection<Type> registered)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(hubName);
            builder.Append(" 에 ");
            builder.Append(wanted.Name);
            builder.Append(" 이 없다. 등록된 것 : ");

            if (registered == null || registered.Count == 0)
            {
                builder.Append("없음");
                return builder.ToString();
            }

            bool first = true;
            foreach (Type type in registered)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }

                builder.Append(type.Name);
            }

            return builder.ToString();
        }
    }
}
