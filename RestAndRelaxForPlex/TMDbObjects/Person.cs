using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TMDbObjects
{
    public class Person : TMDbObjectWithIMDBId
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        internal static Person ClonePerson(Person person)
        {
            return JsonConvert.DeserializeObject<Person>(JsonConvert.SerializeObject(person));
        }
    }
}
