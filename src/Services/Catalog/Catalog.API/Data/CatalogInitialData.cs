namespace Catalog.API.Data
{
  public class CatalogInitialData : IInitialData
  {
    public async Task Populate(IDocumentStore store, CancellationToken cancellation)
    {
      using var session = store.LightweightSession();
      if (await session.Query<Product>().AnyAsync())
      {
        return;
      }

      // Marten UPSERT will cater for existing records
      session.Store<Product>(GetPreconfiguredProducts());
      await session.SaveChangesAsync();
    }

    private static IEnumerable<Product> GetPreconfiguredProducts() => new List<Product>
    {
      new Product
      {
        Id = new Guid("3428D3DE-492F-4187-95CE-4BA31CB6E80B"),
        Name = "iPhone 13",
        Description = "Telefons Apple iPhone 13 128GB Starlight MLPG3ET/A",
        ImageFile = "product-1.png",
        Price = 609,
        Category = new List<string>{"Smart Phone"}
      },
      new Product
      {
        Id = new Guid("143561F7-71C6-4ED8-B3AA-00DF23531981"),
        Name = "iPhone 13",
        Description = "Telefons Apple iPhone 13 128GB Midnight MLPF3ET/A",
        ImageFile = "product-2.png",
        Price = 609,
        Category = new List<string>{"Smart Phone"}
      },
      new Product
      {
        Id = new Guid("891E24A4-118D-415B-ACD1-A9F2170D0611"),
        Name = "iPhone 15",
        Description = "Telefons Apple iPhone 15 128GB Black MTP03PX/A",
        ImageFile = "product-3.png",
        Price = 843,
        Category = new List<string>{"Smart Phone"}
      },
      new Product
      {
        Id = new Guid("578DD20F-54BA-44AD-8204-E319C38F7C0C"),
        Name = "iPhone 15",
        Description = "Telefons Apple iPhone 15 256GB Pink MTP73PX/A",
        ImageFile = "product-4.png",
        Price = 939,
        Category = new List<string>{"Smart Phone"}
      },
      new Product
      {
        Id = new Guid("6F90F143-A857-4F77-A491-B1BBC9AF10BE"),
        Name = "iPhone 13",
        Description = "Telefons Apple iPhone 13 128GB Pink MLPH3ET/A",
        ImageFile = "product-5.png",
        Price = 609,
        Category = new List<string>{"Smart Phone"}
      },
      new Product
      {
        Id = new Guid("31AF00A9-9DB6-42BC-B1C8-51AD81519849"),
        Name = "iPhone 15",
        Description = "Telefons Apple iPhone 15 128GB Pink MTP13PX/A",
        ImageFile = "product-6.png",
        Price = 809,
        Category = new List<string>{"Smart Phone"}
      },
      new Product
      {
        Id = new Guid("FE9BF547-25BD-4D84-9FF8-FF63026D36C6"),
        Name = "iPhone 15",
        Description = "Telefons Apple iPhone 15 Pro 128GB Black Titanium MTUV3PX/A",
        ImageFile = "product-7.png",
        Price = 1079,
        Category = new List<string>{"Smart Phone"}
      }
    };
  }
}
