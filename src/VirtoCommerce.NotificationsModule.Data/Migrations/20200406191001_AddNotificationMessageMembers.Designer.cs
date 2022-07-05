// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VirtoCommerce.NotificationsModule.Data.Repositories;

namespace VirtoCommerce.NotificationsModule.Data.Migrations
{
    [DbContext(typeof(NotificationDbContext))]
    [Migration("20200406191001_AddNotificationMessageMembers")]
    partial class AddNotificationMessageMembers
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailAttachmentEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("FileName")
                        .HasColumnType("nvarchar(512)")
                        .HasMaxLength(512);

                    b.Property<string>("LanguageCode")
                        .HasColumnType("nvarchar(10)")
                        .HasMaxLength(10);

                    b.Property<string>("MimeType")
                        .HasColumnType("nvarchar(50)")
                        .HasMaxLength(50);

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("NotificationId")
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Size")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("Url")
                        .HasColumnType("nvarchar(2048)")
                        .HasMaxLength(2048);

                    b.HasKey("Id");

                    b.HasIndex("NotificationId");

                    b.ToTable("NotificationEmailAttachment");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationEmailRecipientEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("EmailAddress")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("NotificationId")
                        .HasColumnType("nvarchar(128)");

                    b.Property<int>("RecipientType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("NotificationId");

                    b.ToTable("NotificationEmailRecipient");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Kind")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("OuterId")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("TenantId")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("TenantType")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("Type")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.HasKey("Id");

                    b.HasIndex("Type", "TenantId", "TenantType");

                    b.ToTable("Notification");

                    b.HasDiscriminator<string>("Discriminator").HasValue("NotificationEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationMessageEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("LanguageCode")
                        .HasColumnType("nvarchar(10)")
                        .HasMaxLength(10);

                    b.Property<DateTime?>("LastSendAttemptDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastSendError")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("MaxSendAttemptCount")
                        .HasColumnType("int");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("NotificationId")
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("NotificationType")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<int>("SendAttemptCount")
                        .HasColumnType("int");

                    b.Property<DateTime?>("SendDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("TenantId")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("TenantType")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.HasKey("Id");

                    b.HasIndex("NotificationId");

                    b.ToTable("NotificationMessage");

                    b.HasDiscriminator<string>("Discriminator").HasValue("NotificationMessageEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationTemplateEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("CreatedBy")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("LanguageCode")
                        .HasColumnType("nvarchar(10)")
                        .HasMaxLength(10);

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("nvarchar(64)")
                        .HasMaxLength(64);

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("NotificationId")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("OuterId")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.HasKey("Id");

                    b.HasIndex("NotificationId");

                    b.ToTable("NotificationTemplate");

                    b.HasDiscriminator<string>("Discriminator").HasValue("NotificationTemplateEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity");

                    b.Property<string>("From")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("To")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.HasDiscriminator().HasValue("EmailNotificationEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.SmsNotificationEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity");

                    b.Property<string>("Number")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.HasDiscriminator().HasValue("SmsNotificationEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationMessageEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationMessageEntity");

                    b.Property<string>("BCC")
                        .HasColumnType("nvarchar(1024)")
                        .HasMaxLength(512);

                    b.Property<string>("Body")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CC")
                        .HasColumnType("nvarchar(1024)")
                        .HasMaxLength(512);

                    b.Property<string>("From")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.Property<string>("Subject")
                        .HasColumnType("nvarchar(512)")
                        .HasMaxLength(512);

                    b.Property<string>("To")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.HasDiscriminator().HasValue("EmailNotificationMessageEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.SmsNotificationMessageEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationMessageEntity");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Number")
                        .HasColumnType("nvarchar(128)")
                        .HasMaxLength(128);

                    b.HasDiscriminator().HasValue("SmsNotificationMessageEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationTemplateEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationTemplateEntity");

                    b.Property<string>("Body")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Subject")
                        .HasColumnType("nvarchar(512)")
                        .HasMaxLength(512);

                    b.HasDiscriminator().HasValue("EmailNotificationTemplateEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.SmsNotificationTemplateEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationTemplateEntity");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.HasDiscriminator().HasValue("SmsNotificationTemplateEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailAttachmentEntity", b =>
                {
                    b.HasOne("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationEntity", "Notification")
                        .WithMany("Attachments")
                        .HasForeignKey("NotificationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationEmailRecipientEntity", b =>
                {
                    b.HasOne("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationEntity", "Notification")
                        .WithMany("Recipients")
                        .HasForeignKey("NotificationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationMessageEntity", b =>
                {
                    b.HasOne("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity", "Notification")
                        .WithMany()
                        .HasForeignKey("NotificationId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationTemplateEntity", b =>
                {
                    b.HasOne("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity", "Notification")
                        .WithMany("Templates")
                        .HasForeignKey("NotificationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
