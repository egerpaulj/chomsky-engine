//      Microservice AMQP Libraries for .Net C#
//      Copyright (C) 2021  Paul Eger

//      This program is free software: you can redistribute it and/or modify
//      it under the terms of the GNU General Public License as published by
//      the Free Software Foundation, either version 3 of the License, or
//      (at your option) any later version.

//      This program is distributed in the hope that it will be useful,
//      but WITHOUT ANY WARRANTY; without even the implied warranty of
//      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//      GNU General Public License for more details.

//      You should have received a copy of the GNU General Public License
//      along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Threading.Tasks;
using LanguageExt;

namespace Microservice.Amqp
{
    /// <summary>
    /// The AMQP Message handler that handles messages.
    /// The Input T, and Output R is coupled to the Subscriber's Input message Type T and Output R.
    /// </summary>
    public interface IMessageHandler<T, R>
    {
        Task<R> HandleMessage(Option<Message<T>> message);
    }
}
