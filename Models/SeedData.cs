using Microsoft.EntityFrameworkCore;

namespace SalonHair.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new SalonContext(
                serviceProvider.GetRequiredService<DbContextOptions<SalonContext>>()))
            {
                if (!context.Services.Any())
                {
                    context.Services.AddRange(
                        new Service
                        {
                            ServiceName = "Cắt tóc nam",
                            Price = 100000,
                            Description = "Cắt tóc nam trẻ trung, hiện đại."
                        },
                        new Service
                        {
                            ServiceName = "Cắt tóc nữ",
                            Price = 200000,
                            Description = "Cắt tóc nữ tạo kiểu thời trang."
                        },
                        new Service
                        {
                            ServiceName = "Nhuộm tóc Luxury",
                            Price = 500000,
                            Description = "Nhuộm màu chuẩn, không hư tổn tóc."
                        },
                        new Service
                        {
                            ServiceName = "Uốn tóc cao cấp",
                            Price = 800000,
                            Description = "Uốn xoăn, bồng bềnh quyến rũ."
                        },
                        new Service
                        {
                            ServiceName = "Gội đầu thảo dược",
                            Price = 50000,
                            Description = "Thư giãn cùng tinh chất tự nhiên."
                        },
                        new Service
                        {
                            ServiceName = "Hấp dầu phục hồi",
                            Price = 300000,
                            Description = "Nuôi dưỡng tóc từ sâu bên trong."
                        }
                    );
                }

                if (context.Hairstyles.Count() < 12)
                {
                    var oldHairstyles = context.Hairstyles.ToList();
                    context.Hairstyles.RemoveRange(oldHairstyles);
                    context.SaveChanges();

                    context.Hairstyles.AddRange(
                        new Hairstyle { StyleName = "High Fade Pompadour", FaceShape = "Tròn", Gender = "Nam", AgeGroup = "18-30, 31-45", Description = "Tạo độ cao cho khuôn mặt, làm mặt trông dài và thon gọn hơn.", ImageUrl = "https://i.pinimg.com/564x/49/73/7a/49737a281804c86576b509f61b0c0342.jpg" },
                        new Hairstyle { StyleName = "Layer Layer", FaceShape = "Tròn", Gender = "Nữ", AgeGroup = "Dưới 18, 18-30", Description = "Lớp tóc tỉa giúp che bớt độ rộng hai bên má.", ImageUrl = "https://i.pinimg.com/564x/87/42/85/8742857476579895655.jpg" },
                        new Hairstyle { StyleName = "Side Part 7/3", FaceShape = "Tròn", Gender = "Nam", AgeGroup = "18-30, 31-45, 46+", Description = "Phong cách cổ điển, lịch lãm phù hợp mọi lứa tuổi.", ImageUrl = "https://i.pinimg.com/564x/23/e4/77/23e477651ad36a7a5601a35565551.jpg" },

                        new Hairstyle { StyleName = "Crew Cut", FaceShape = "Vuông", Gender = "Nam", AgeGroup = "18-30, 31-45, 46+", Description = "Nam tính, gọn gàng và tôn lên đường nét xương hàm.", ImageUrl = "https://i.pinimg.com/564x/07/77/80/0777800c0f9942d93699c824f11b2685.jpg" },
                        new Hairstyle { StyleName = "Undercut vuốt ngược", FaceShape = "Vuông", Gender = "Nam", AgeGroup = "Dưới 18, 18-30", Description = "Kiểu tóc hot trend giúp tôn vẻ góc cạnh mạnh mẽ.", ImageUrl = "https://i.pinimg.com/564x/1a/2b/3c/1a2b3c4d5e6f7g8h9i0j.jpg" },
                        new Hairstyle { StyleName = "Ivy League", FaceShape = "Vuông", Gender = "Nam", AgeGroup = "18-30, 31-45", Description = "Mẫu mực và sang trọng cho quý ông công sở.", ImageUrl = "https://i.pinimg.com/564x/a1/b1/c1/a1b1c1d1e1f1g1h1i1j1.jpg" },

                        new Hairstyle { StyleName = "Buzz Cut", FaceShape = "Trái xoan", Gender = "Nam", AgeGroup = "Dưới 18, 18-30", Description = "Khoe trọn vẹn gương mặt cân đối hoàn hảo.", ImageUrl = "https://i.pinimg.com/564x/01/02/03/01020304050607080910.jpg" },
                        new Hairstyle { StyleName = "Mullet thời thượng", FaceShape = "Trái xoan", Gender = "Nam", AgeGroup = "Dưới 18, 18-30", Description = "Kiểu tóc cá tính và đầy phá cách.", ImageUrl = "https://i.pinimg.com/564x/11/12/13/11121314151617181920.jpg" },
                        new Hairstyle { StyleName = "Uốn xoăn nhẹ", FaceShape = "Trái xoan", Gender = "Nữ", AgeGroup = "18-30, 31-45", Description = "Tạo vẻ lãng tử và trẻ trung.", ImageUrl = "https://i.pinimg.com/564x/21/22/23/21222324252627282930.jpg" },

                        new Hairstyle { StyleName = "Middle Part (Bổ luống)", FaceShape = "Dài", Gender = "Nam", AgeGroup = "Dưới 18, 18-30", Description = "Cân bằng độ dài khuôn mặt, tạo sự hài hòa.", ImageUrl = "https://i.pinimg.com/564x/31/32/33/31323334353637383940.jpg" },
                        new Hairstyle { StyleName = "Side Swept", FaceShape = "Dài", Gender = "Nam", AgeGroup = "18-30, 31-45", Description = "Tóc vuốt lệch một bên giúp mặt bớt cảm giác quá dài.", ImageUrl = "https://i.pinimg.com/564x/41/42/43/41424344454647484950.jpg" },
                        new Hairstyle { StyleName = "Tóc Mái (Fringe)", FaceShape = "Dài", Gender = "Nam", AgeGroup = "Dưới 18, 18-30, 31-45", Description = "Che bớt phần trán giúp khuôn mặt cân đối hơn.", ImageUrl = "https://i.pinimg.com/564x/51/52/53/51525354555657585960.jpg" }
                    );
                }

                if (!context.Products.Any())
                {
                    context.Products.AddRange(
                        new Product { Name = "Sáp vuốt tóc Volcanic Clay", Price = 250000, Description = "Độ giữ nếp cao, không bóng, dễ dàng gội rửa, phù hợp cho mọi loại tóc.", ImageUrl = "https://bizweb.dktcdn.net/thumb/1024x1024/100/171/314/products/sap-vuot-toc-volcanic-clay-chinh-hang.jpg" },
                        new Product { Name = "Gôm xịt tóc Butterfly Shadow", Price = 150000, Description = "Giữ nếp cực tốt, mùi hương trái cây nhẹ nhàng, dễ chịu.", ImageUrl = "https://bizweb.dktcdn.net/100/171/314/products/gom-xit-toc-butterfly-shadow-5.jpg" },
                        new Product { Name = "Dầu dưỡng tóc Moroccanoil", Price = 750000, Description = "Phục hồi tóc hư tổn, mang lại độ bóng mượt vượt trội.", ImageUrl = "https://bizweb.dktcdn.net/100/171/314/products/tinh-dau-duong-toc-moroccanoil-treatment.jpg" },
                        new Product { Name = "Sáp vuốt tóc Kevin Murphy", Price = 650000, Description = "Chất sáp cao cấp, bảo vệ tóc và giữ nếp hoàn hảo.", ImageUrl = "https://bizweb.dktcdn.net/100/171/314/products/sap-vuot-toc-kevin-murphy-rough-rider-100g.jpg" }
                    );
                }

                var adminUser = context.Users.FirstOrDefault(u => u.Username == "admin");
                if (adminUser == null)
                {
                    context.Users.Add(new User
                    {
                        Username = "admin",
                        Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                        RoleId = 2,
                        Email = "admin@salonhair.com",
                        IsEmailVerified = true,
                        IsLocked = false
                    });
                }
                else
                {
                    adminUser.RoleId = 2;
                    adminUser.Password = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                    adminUser.Email = "admin@salonhair.com";
                    adminUser.IsEmailVerified = true;
                    adminUser.IsLocked = false;
                    context.Users.Update(adminUser);
                }

                var normalUser = context.Users.FirstOrDefault(u => u.Username == "user");
                if (normalUser == null)
                {
                    context.Users.Add(new User
                    {
                        Username = "user",
                        Password = BCrypt.Net.BCrypt.HashPassword("User@123"),
                        RoleId = 1,
                        Email = "user@salonhair.com",
                        IsEmailVerified = true,
                        IsLocked = false
                    });
                }
                else
                {
                    normalUser.RoleId = 1;
                    normalUser.Password = BCrypt.Net.BCrypt.HashPassword("User@123");
                    normalUser.Email = "user@salonhair.com";
                    normalUser.IsEmailVerified = true;
                    normalUser.IsLocked = false;
                    context.Users.Update(normalUser);
                }

                context.SaveChanges();
            }
        }
    }
}