//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2022  Paul Eger                                                                                                                                                                     

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
using System;
using LanguageExt;
using Microservice.Exchange;

namespace Microservice.Exchange.Test
{
    public class TestEsDataTransformer : ITransformer<string, TestEsOutputMessage>
    {
        internal const string TestData = "Some test data";

        public TryOptionAsync<Message<TestEsOutputMessage>> Transform(Option<Message<string>> input)
        {
            return input.ToTryOptionAsync().Map(message =>
            {
                return message.CopyData(new Message<TestEsOutputMessage>
                {
                    Payload = new TestEsOutputMessage()
                    {
                        Id = message.Id.Match(i => i, () => Guid.NewGuid()),
                        //TestId = message.Id.Match(i => i, () => Guid.NewGuid()),
                        OriginalData = message.Payload.Match(r => r, () => "Wrong message"),
                        EnrichedData = TestData
                    }
                });
            }); 
        }
    }
}