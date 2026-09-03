using QuickClassMap.Core.Domain;

namespace QuickClassMap.Tests;

public class RoslynClassParserTests(RoslynClassParserFixture fixture)
    : IClassFixture<RoslynClassParserFixture>
{
    [Fact]
    public void Parse_WithClassDefinitionsAndInheritance_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Customer
                {
                }

                public class PreferredCustomer : Customer
                {
                }
            }
            """;

        var classes = fixture.Parse(source);
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var preferredCustomer = Assert.Single(classes, classInfo => classInfo.Name == "PreferredCustomer");
        var relationship = Assert.Single(preferredCustomer.Relationships);

        Assert.Equal("Customer", customer.Name);
        Assert.Equal("Sample.Customer", customer.FullName);
        Assert.False(customer.IsInterface);
        Assert.Equal("Sample.Customer", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Inherits, relationship.Type);
    }

    [Fact]
    public void Parse_WithMethodSignature_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Order
                {
                    public Customer SetCustomer(Customer customer)
                    {
                        return customer;
                    }
                }

                public class Customer
                {
                }
            }
            """;

        var classes = fixture.Parse(source);
        var order = Assert.Single(classes, classInfo => classInfo.Name == "Order");
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var relationship = Assert.Single(order.Relationships);

        Assert.Equal("Order", order.Name);
        Assert.Equal("Sample.Order", order.FullName);
        Assert.False(order.IsInterface);
        Assert.Equal("Customer", customer.Name);
        Assert.Equal("Sample.Customer", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Uses, relationship.Type);
    }

    [Fact]
    public void Parse_WithInterfaceImplementation_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public interface ICustomer
                {
                }

                public interface IEntity
                {
                }

                public class Customer : ICustomer, IEntity
                {
                }
            }
            """;

        var classes = fixture.Parse(source);
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var customerInterface = Assert.Single(classes, classInfo => classInfo.Name == "ICustomer");
        var entityInterface = Assert.Single(classes, classInfo => classInfo.Name == "IEntity");

        Assert.Equal("Sample.Customer", customer.FullName);
        Assert.False(customer.IsInterface);
        Assert.Equal("ICustomer", customerInterface.Name);
        Assert.Equal("Sample.ICustomer", customerInterface.FullName);
        Assert.True(customerInterface.IsInterface);
        Assert.Equal("IEntity", entityInterface.Name);
        Assert.True(entityInterface.IsInterface);
        Assert.Equal(2, customer.Relationships.Count);
        Assert.Contains(customer.Relationships, relationship =>
            relationship.RelatedClassName == "Sample.ICustomer" && relationship.Type == RelationshipType.Implements);
        Assert.Contains(customer.Relationships, relationship =>
            relationship.RelatedClassName == "Sample.IEntity" && relationship.Type == RelationshipType.Implements);
    }

    [Fact]
    public void Parse_WithRecordDefinitions_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public record CustomerRecord;
                public record PreferredCustomerRecord : CustomerRecord;
            }
            """;

        var classes = fixture.Parse(source);
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "CustomerRecord");
        var preferredCustomer = Assert.Single(classes, classInfo => classInfo.Name == "PreferredCustomerRecord");
        var relationship = Assert.Single(preferredCustomer.Relationships);

        Assert.Equal("Sample.CustomerRecord", customer.FullName);
        Assert.False(customer.IsInterface);
        Assert.Equal("Sample.PreferredCustomerRecord", preferredCustomer.FullName);
        Assert.False(preferredCustomer.IsInterface);
        Assert.Equal("Sample.CustomerRecord", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Inherits, relationship.Type);
    }

    [Fact]
    public void Parse_WithFieldsAndProperties_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Customer
                {
                }

                public class AggregatingOwner
                {
                    public Customer Customer
                    {
                        get { return null; }
                    }
                }

                public class ComposingOwner
                {
                    private Customer customer;
                }

                public class ConstructorOwner
                {
                    private Customer Customer
                    {
                        get { return null; }
                    }

                    public ConstructorOwner(Customer customer)
                    {
                    }
                }
            }
            """;

        var classes = fixture.Parse(source);
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var aggregatingOwner = Assert.Single(classes, classInfo => classInfo.Name == "AggregatingOwner");
        var composingOwner = Assert.Single(classes, classInfo => classInfo.Name == "ComposingOwner");
        var constructorOwner = Assert.Single(classes, classInfo => classInfo.Name == "ConstructorOwner");

        Assert.Equal("Sample.Customer", customer.FullName);
        Assert.False(customer.IsInterface);
        Assert.Equal(RelationshipType.Aggregates, Assert.Single(aggregatingOwner.Relationships).Type);
        Assert.Equal(RelationshipType.Composes, Assert.Single(composingOwner.Relationships).Type);
        Assert.Equal(RelationshipType.Aggregates, Assert.Single(constructorOwner.Relationships).Type);
        Assert.All(aggregatingOwner.Relationships, relationship => Assert.Equal("Sample.Customer", relationship.RelatedClassName));
        Assert.All(composingOwner.Relationships, relationship => Assert.Equal("Sample.Customer", relationship.RelatedClassName));
        Assert.All(constructorOwner.Relationships, relationship => Assert.Equal("Sample.Customer", relationship.RelatedClassName));
    }

    [Fact]
    public void Parse_WithConstructorParameter_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Order
                {
                    public Order(Customer customer)
                    {
                    }
                }

                public class Customer
                {
                }
            }
            """;

        var classes = fixture.Parse(source);
        var order = Assert.Single(classes, classInfo => classInfo.Name == "Order");
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var relationship = Assert.Single(order.Relationships);

        Assert.Equal("Sample.Order", order.FullName);
        Assert.Equal("Sample.Customer", customer.FullName);
        Assert.Equal("Sample.Customer", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Aggregates, relationship.Type);
    }

    [Fact]
    public void Parse_WithCollectionAndGenericMembers_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Customer
                {
                }

                public class Box<T>
                {
                }

                public class CollectionOwner
                {
                    private System.Collections.Generic.List<Customer> customers;
                }

                public class GenericOwner
                {
                    private Box<Customer> box;
                }
            }
            """;

        var classes = fixture.Parse(source);
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var box = Assert.Single(classes, classInfo => classInfo.Name == "Box<T>");
        var collectionOwner = Assert.Single(classes, classInfo => classInfo.Name == "CollectionOwner");
        var genericOwner = Assert.Single(classes, classInfo => classInfo.Name == "GenericOwner");

        Assert.Equal("Sample.Customer", customer.FullName);
        Assert.Equal("Sample.Box<T>", box.FullName);
        Assert.False(box.IsInterface);
        Assert.Equal(RelationshipType.Aggregates, Assert.Single(collectionOwner.Relationships).Type);
        Assert.Equal(2, genericOwner.Relationships.Count);
        Assert.Contains(genericOwner.Relationships, relationship =>
            relationship.RelatedClassName == "Sample.Customer" && relationship.Type == RelationshipType.Composes);
        Assert.Contains(genericOwner.Relationships, relationship =>
            relationship.RelatedClassName == "Sample.Box<T>" && relationship.Type == RelationshipType.Composes);
    }

    [Fact]
    public void Parse_WithMethodInvocation_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Service
                {
                    public static void Execute()
                    {
                    }
                }

                public class Client
                {
                    public void Run()
                    {
                        Service.Execute();
                    }
                }
            }
            """;

        var classes = fixture.Parse(source);
        var service = Assert.Single(classes, classInfo => classInfo.Name == "Service");
        var client = Assert.Single(classes, classInfo => classInfo.Name == "Client");
        var relationship = Assert.Single(client.Relationships);

        Assert.Equal("Sample.Service", service.FullName);
        Assert.Equal("Sample.Client", client.FullName);
        Assert.Equal("Sample.Service", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Uses, relationship.Type);
    }

    [Fact]
    public void Parse_WithGenericMethodInvocation_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Customer
                {
                }

                public class GenericService
                {
                    public static void Execute<T>()
                    {
                    }
                }

                public class Client
                {
                    public void Run()
                    {
                        GenericService.Execute<Customer>();
                    }
                }
            }
            """;

        var classes = fixture.Parse(source);
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var genericService = Assert.Single(classes, classInfo => classInfo.Name == "GenericService");
        var client = Assert.Single(classes, classInfo => classInfo.Name == "Client");

        Assert.Equal("Sample.Customer", customer.FullName);
        Assert.Equal("Sample.GenericService", genericService.FullName);
        Assert.Equal("Sample.Client", client.FullName);
        Assert.Equal(2, client.Relationships.Count);
        Assert.Contains(client.Relationships, relationship =>
            relationship.RelatedClassName == "Sample.GenericService" && relationship.Type == RelationshipType.Uses);
        Assert.Contains(client.Relationships, relationship =>
            relationship.RelatedClassName == "Sample.Customer" && relationship.Type == RelationshipType.Uses);
    }

    [Fact]
    public void Parse_WithObjectCreation_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Product
                {
                }

                public class Factory
                {
                    public Product Create()
                    {
                        return new Product();
                    }
                }

                public class StaticFactory
                {
                    public static void Create()
                    {
                        var product = new Product();
                    }
                }
            }
            """;

        var classes = fixture.Parse(source);
        var product = Assert.Single(classes, classInfo => classInfo.Name == "Product");
        var factory = Assert.Single(classes, classInfo => classInfo.Name == "Factory");
        var staticFactory = Assert.Single(classes, classInfo => classInfo.Name == "StaticFactory");
        var relationship = Assert.Single(factory.Relationships);

        Assert.Equal("Sample.Product", product.FullName);
        Assert.Equal("Sample.Factory", factory.FullName);
        Assert.Equal("Sample.StaticFactory", staticFactory.FullName);
        Assert.Equal("Sample.Product", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Composes, relationship.Type);
        Assert.Empty(staticFactory.Relationships);
    }

    [Fact]
    public void Parse_WithTypeConversions_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Customer
                {
                }

                public class Converter
                {
                    public void Convert(object value)
                    {
                        var castCustomer = (Customer)value;
                        var customerArray = (Customer[])value;
                        var asCustomer = value as Customer;

                        if (value is Customer)
                        {
                        }
                    }
                }
            }
            """;

        var classes = fixture.Parse(source);
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var converter = Assert.Single(classes, classInfo => classInfo.Name == "Converter");
        var relationship = Assert.Single(converter.Relationships);

        Assert.Equal("Sample.Customer", customer.FullName);
        Assert.Equal("Sample.Converter", converter.FullName);
        Assert.Equal("Sample.Customer", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Uses, relationship.Type);
    }

    [Fact]
    public void Parse_WithLambdas_ReturnsClassesAndRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Customer
                {
                }

                public class Processor
                {
                    public void Process()
                    {
                        System.Func<Customer, bool> simple = customer => true;
                        System.Func<Customer, bool> parenthesized = (Customer customer) => true;
                    }
                }
            }
            """;

        var classes = fixture.Parse(source);
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var processor = Assert.Single(classes, classInfo => classInfo.Name == "Processor");
        var relationship = Assert.Single(processor.Relationships);

        Assert.Equal("Sample.Customer", customer.FullName);
        Assert.Equal("Sample.Processor", processor.FullName);
        Assert.Equal("Sample.Customer", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Uses, relationship.Type);
    }

    [Fact]
    public void Parse_WithInheritedTypesUsedInMethodBody_ReturnsOnlyInheritanceRelationship()
    {
        const string source = """
            namespace Sample
            {
                public class Base
                {
                    public static void Execute()
                    {
                    }
                }

                public class Derived : Base
                {
                    public void Run()
                    {
                        Execute();
                        var baseInstance = new Base();
                        var converted = new object() as Base;
                    }
                }
            }
            """;

        var classes = fixture.Parse(source);
        var baseClass = Assert.Single(classes, classInfo => classInfo.Name == "Base");
        var derived = Assert.Single(classes, classInfo => classInfo.Name == "Derived");
        var relationship = Assert.Single(derived.Relationships);

        Assert.Equal("Sample.Base", baseClass.FullName);
        Assert.Equal("Sample.Derived", derived.FullName);
        Assert.Equal("Sample.Base", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Inherits, relationship.Type);
    }

    [Fact]
    public void Parse_WithInheritedTypesFromMultipleLevelsUsedInMethodBody_ReturnsOnlyInheritanceRelationship()
    {
        const string source = """
            namespace Sample
            {
                public class Base
                {
                    public static void Execute()
                    {
                    }
                }

                public class Intermediate : Base
                {
                }

                public class Derived : Intermediate
                {
                    public void Run()
                    {
                        Execute();
                    }
                }
            }
            """;

        var classes = fixture.Parse(source);
        var baseClass = Assert.Single(classes, classInfo => classInfo.Name == "Base");
        var intermediate = Assert.Single(classes, classInfo => classInfo.Name == "Intermediate");
        var derived = Assert.Single(classes, classInfo => classInfo.Name == "Derived");
        var intermediateRelationship = Assert.Single(intermediate.Relationships);
        var derivedRelationship = Assert.Single(derived.Relationships);

        Assert.Equal("Sample.Base", baseClass.FullName);
        Assert.Equal("Sample.Intermediate", intermediate.FullName);
        Assert.Equal("Sample.Derived", derived.FullName);
        Assert.Equal("Sample.Base", intermediateRelationship.RelatedClassName);
        Assert.Equal(RelationshipType.Inherits, intermediateRelationship.Type);
        Assert.Equal("Sample.Intermediate", derivedRelationship.RelatedClassName);
        Assert.Equal(RelationshipType.Inherits, derivedRelationship.Type);
    }

    [Fact]
    public void Parse_WithSelfAndExternalTypes_DoesNotCreateRelationships()
    {
        const string source = """
            namespace Sample
            {
                public class Node
                {
                    public Node Create()
                    {
                        return new Node();
                    }

                    public void Run()
                    {
                        Run();
                    }

                    public string Name { get; set; }
                }

                public class Other
                {
                }
            }
            """;

        var classes = fixture.Parse(source);
        var node = Assert.Single(classes, classInfo => classInfo.Name == "Node");
        var other = Assert.Single(classes, classInfo => classInfo.Name == "Other");

        Assert.Equal("Sample.Node", node.FullName);
        Assert.Equal("Sample.Other", other.FullName);
        Assert.Empty(node.Relationships);
    }

    [Fact]
    public void Parse_WithDuplicateRelationships_KeepsStrongerType()
    {
        const string source = """
            namespace Sample
            {
                public class Customer
                {
                }

                public class Owner
                {
                    private Customer customer;

                    public void Use(Customer customer)
                    {
                    }
                }
            }
            """;

        var classes = fixture.Parse(source);
        var customer = Assert.Single(classes, classInfo => classInfo.Name == "Customer");
        var owner = Assert.Single(classes, classInfo => classInfo.Name == "Owner");
        var relationship = Assert.Single(owner.Relationships);

        Assert.Equal("Sample.Customer", customer.FullName);
        Assert.Equal("Sample.Owner", owner.FullName);
        Assert.Equal("Sample.Customer", relationship.RelatedClassName);
        Assert.Equal(RelationshipType.Composes, relationship.Type);
    }
}
