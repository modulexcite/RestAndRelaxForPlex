using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TmdbObjects
{
    public class Person : TmdbObjectWithImdbId
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        internal static Person ClonePerson(Person person)
        {
            return JsonConvert.DeserializeObject<Person>(JsonConvert.SerializeObject(person));
        }
    }
}
