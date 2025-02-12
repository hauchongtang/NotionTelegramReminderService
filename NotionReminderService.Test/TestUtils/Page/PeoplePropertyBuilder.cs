using Notion.Client;

namespace NotionReminderService.Test.TestUtils.Page;

public class PeoplePropertyBuilder
{
	private readonly List<User> _personsToAdd = new ();
	
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