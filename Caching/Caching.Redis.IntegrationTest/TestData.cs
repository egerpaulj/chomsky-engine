//      Microservice Cache Libraries for .Net C#                                                                                                                                       
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

using System;

namespace Caching.Redis.IntegrationTest
{
    public class TestData
    {
        public Guid Id {get;set;}
        public string Data {get;set;}

        public override bool Equals(object obj)
        {
            if (obj is TestData)
            {
                var other = obj as TestData;

                if(other.Data == Data && other.Id.ToString() == Id.ToString())
                    return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() + Data.GetHashCode();
        }
    }
}