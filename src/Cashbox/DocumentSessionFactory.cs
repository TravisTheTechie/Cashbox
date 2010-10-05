namespace Cashbox
{
	public class DocumentSessionFactory
	{
		public static IDocumentSession Create(string filename)
		{
			return new DocumentSession(filename);
		}
	}
}