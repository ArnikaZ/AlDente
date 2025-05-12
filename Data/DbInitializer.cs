using AlDentev2.Models;
using AlDentev2.Pages;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            if (await context.Products.AnyAsync())
            {
                return;
            }


            var categories = new List<Category>
            {
                new Category {Name="Bluzy"},
                new Category {Name="Koszulki"},
                new Category {Name="Czapki"},
                new Category {Name="Torby"}
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            var sizes = new List<Size>
            {
                new Size {Name="S"},
                new Size {Name="M"},
                new Size {Name="L"},
                new Size {Name="XL"}
            };
            await context.Sizes.AddRangeAsync(sizes);
            await context.SaveChangesAsync();

            var products = new List<Product>
            {
                new Product
                {
                    Name="AL DENTE HOODIE WHITE",
                    Description="Biała bluza z kapturem z logo AL DENTE",
                    Price=399.00M,
                    ImageUrl="/images/hoodie-white1.jpg",
                    CategoryId=categories.First(c=>c.Name=="Bluzy").Id,
                    SKU="HOO-WHT-001"
                },
                new Product
                {
                    Name="AL DENTE HOODIE BLACK",
                    Description="Czarna bluza z kapturem z logo AL DENTE",
                    Price=399.00M,
                    ImageUrl="/images/hoodie-black1.jpg",
                    CategoryId=categories.First(c=>c.Name=="Bluzy").Id,
                    SKU="HOO-BLK-001"
                },
                new Product
                {
                    Name="AL DENTE HOODIE OFF-WHITE",
                    Description="Kremowa bluza z kapturem z logo AL DENTE",
                    Price=399.00M,
                    ImageUrl="/images/hoodie-offwhite1.jpg",
                    CategoryId=categories.First(c=>c.Name=="Bluzy").Id,
                    SKU="HOO-OFW-001"
                },
                new Product
                {
                    Name="AL DENTE T-SHIRT WHITE",
                    Description="Biały t-shirt z logo AL DENTE",
                    Price=199.00M,
                    ImageUrl="/images/shirt-white1.jpg",
                    CategoryId=categories.First(c=>c.Name=="Koszulki").Id,
                    SKU="TSH-WHT-001"
                },
                new Product
                {
                    Name="AL DENTE T-SHIRT BLACK",
                    Description="Czarny t-shirt z logo AL DENTE",
                    Price=199.00M,
                    ImageUrl="/images/shirt-black1.jpg",
                    CategoryId=categories.First(c=>c.Name=="Koszulki").Id,
                    SKU="TSH-BLK-001"
                },
                 new Product
                {
                    Name = "AL DENTE HAT WHITE",
                    Description = "Biała czapka z logo AL DENTE",
                    Price = 99.00M,
                    ImageUrl = "/images/hat1.jpg",
                    CategoryId = categories.First(c => c.Name == "Czapki").Id,
                    SKU = "HAT-WHT-001"
                },
                new Product
                {
                    Name = "AL DENTE BAG WHITE",
                    Description = "Biała torba z logo AL DENTE",
                    Price = 99.00M,
                    ImageUrl = "/images/bag-white1.jpg",
                    CategoryId = categories.First(c => c.Name == "Torby").Id,
                    SKU = "BAG-WHT-001"
                },
                new Product
                {
                    Name = "AL DENTE BAG BLACK",
                    Description = "Czarna torba z logo AL DENTE",
                    Price = 99.00M,
                    ImageUrl = "/images/bag-black1.jpg",
                    CategoryId = categories.First(c => c.Name == "Torby").Id,
                    SKU = "BAG-BLK-001"
                }
            };
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            var productImages = new List<ProductImage>();
            foreach(var product in products)
            {
                productImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = product.ImageUrl,
                    IsMain = true,
                    DisplayOrder = 0
                });
                if (product.CategoryId == categories.First(c => c.Name == "Bluzy").Id)
                {
                    if (product.Name.Contains("OFF-WHITE")) 
                    {
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/hoodie-offwhite2.jpg",
                            DisplayOrder = 1
                        });
                        
                    }
                    else if (product.Name.Contains("BLACK"))
                    {
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/hoodie-black2.jpg",
                            DisplayOrder = 1
                        });
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/hoodie-black3.jpg",
                            DisplayOrder = 2
                        });
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/hoodie-black4.jpg",
                            DisplayOrder = 3
                        });
                    }
                    else if (product.Name.Contains("WHITE"))
                    {
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/hoodie-white2.jpg",
                            DisplayOrder = 1
                        });
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/hoodie-white3.jpg",
                            DisplayOrder = 2
                        });
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/hoodie-white4.jpg",
                            DisplayOrder = 3
                        });
                    }
                }
                else if((product.CategoryId == categories.First(c => c.Name == "Koszulki").Id))
                {
                    if (product.Name.Contains("WHITE"))
                    {
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/shirt-white2.jpg",
                            DisplayOrder = 1
                        });
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/shirt-white3.jpg",
                            DisplayOrder = 2
                        });
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/shirt-white4.jpg",
                            DisplayOrder = 3
                        });
                    }
                    else if (product.Name.Contains("BLACK"))
                    {
                        
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/shirt-black3.jpg",
                            DisplayOrder = 1
                        });
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/shirt-black4.jpg",
                            DisplayOrder = 2
                        });
                    }
                }
                else if ((product.CategoryId == categories.First(c => c.Name == "Czapki").Id))
                {
                    productImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = "/images/hat2.jpg",
                        DisplayOrder = 1
                    });
                    
                }
                else if ((product.CategoryId == categories.First(c => c.Name == "Torby").Id))
                {
                    if (product.Name.Contains("WHITE"))
                    {
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/bag-white2.jpg",
                            DisplayOrder=1
                        });
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/bag-white3.jpg",
                            DisplayOrder = 2
                        });
                    }
                    else if (product.Name.Contains("BLACK"))
                    {
                        productImages.Add(new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/bag-black2.jpg",
                            DisplayOrder = 1
                        });
                    }
                }
            }
            await context.ProductImages.AddRangeAsync(productImages);
            await context.SaveChangesAsync();

            var productSizes = new List<ProductSize>();
            foreach(var product in products.Where(p=>p.CategoryId== categories.First(c => c.Name == "Bluzy").Id))
            {
                foreach(var size in sizes)
                {
                    productSizes.Add(new ProductSize
                    {
                        ProductId = product.Id,
                        SizeId = size.Id,
                        StockQuantity = 15 //początkowa ilość w magazynie
                    });
                }
            }
            foreach(var product in products.Where(p => p.CategoryId == categories.First(c => c.Name == "Koszulki").Id))
            {
                foreach(var size in sizes)
                {
                    productSizes.Add(new ProductSize
                    {
                        ProductId = product.Id,
                        SizeId = size.Id,
                        StockQuantity = 15
                    });
                }
            }
            foreach(var product in products.Where(p => p.CategoryId == categories.First(c => c.Name == "Czapki").Id))
            {
                foreach(var size in sizes.Where(s => s.Name != "XL"))
                {
                    productSizes.Add(new ProductSize
                    {
                        ProductId = product.Id,
                        SizeId = size.Id,
                        StockQuantity = 10
                    });
                }
            }
            //foreach (var product in products.Where(p => p.CategoryId == categories.First(c => c.Name == "Torby").Id))
            //{
            //    productSizes.Add(new ProductSize
            //    {
            //        ProductId = product.Id,
            //        SizeId = 5,
            //        StockQuantity = 10
            //    });
            //}
            await context.ProductSizes.AddRangeAsync(productSizes);
            await context.SaveChangesAsync();

            var shippingMethods = new List<ShippingMethod>
            {
                new ShippingMethod
                {
                    Name="Kurier",
                    Description="Dostawa w ciągu 1-2 dni roboczych",
                    Cost=15.00M
                },
                new ShippingMethod
                {
                    Name="Paczkomat",
                    Description= "Dostawa w ciągu 1-3 dni roboczych",
                    Cost=12.00M
                }
            };
            await context.ShippingMethods.AddRangeAsync(shippingMethods);
            await context.SaveChangesAsync();

            var paymentMethods = new List<PaymentMethod>
            {
                new PaymentMethod
                {
                    Name="Przelew bankowy",
                    Description="Płatność przelewem na konto bankowe",
                },
                new PaymentMethod
                {
                    Name="Płatność online",
                    Description="Szybka płatność online przez Przelewy24"
                },
                new PaymentMethod
                {
                    Name="BLIK",
                    Description="Płatność za pomocą kodu BLIK"
                }
            };
            await context.PaymentMethods.AddRangeAsync(paymentMethods);
            await context.SaveChangesAsync();
        }
    }
}
