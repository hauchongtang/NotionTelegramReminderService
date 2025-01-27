public class PeoplePropertyBuilder
{
	private readonly List<User> _personsToAdd;
	
	public PeoplePropertyValue Build()
	{
		return new PeoplePropertyValue {
			People = _personsToAdd
		};
	}
	
	public PeoplePropertyBuilder WithUser(User userToAdd)
	{
		_personsToAdd.Add(userToAdd);
		return this;
	}
}