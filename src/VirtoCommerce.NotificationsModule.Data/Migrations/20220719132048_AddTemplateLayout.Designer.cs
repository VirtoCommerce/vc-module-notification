﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VirtoCommerce.NotificationsModule.Data.Repositories;

#nullable disable

namespace VirtoCommerce.NotificationsModule.Data.Migrations
{
    [DbContext(typeof(NotificationDbContext))]
    [Migration("20220719132048_AddTemplateLayout")]
    partial class AddTemplateLayout
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailAttachmentEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("FileName")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("LanguageCode")
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("MimeType")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("ModifiedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("NotificationId")
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Size")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Url")
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)");

                    b.HasKey("Id");

                    b.HasIndex("NotificationId");

                    b.ToTable("NotificationEmailAttachment", (string)null);
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationEmailRecipientEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("EmailAddress")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("NotificationId")
                        .HasColumnType("nvarchar(128)");

                    b.Property<int>("RecipientType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("NotificationId");

                    b.ToTable("NotificationEmailRecipient", (string)null);
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<bool?>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("Kind")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ModifiedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("OuterId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("TenantId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("TenantType")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Type")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.HasKey("Id");

                    b.HasIndex("Type", "TenantId", "TenantType");

                    b.ToTable("Notification", (string)null);

                    b.HasDiscriminator<string>("Discriminator").HasValue("NotificationEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationLayoutEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("ModifiedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Template")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("NotificationLayout", (string)null);
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationMessageEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("LanguageCode")
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<DateTime?>("LastSendAttemptDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastSendError")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("MaxSendAttemptCount")
                        .HasColumnType("int");

                    b.Property<string>("ModifiedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("NotificationId")
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("NotificationType")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<int>("SendAttemptCount")
                        .HasColumnType("int");

                    b.Property<DateTime?>("SendDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("TenantId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("TenantType")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.HasKey("Id");

                    b.HasIndex("NotificationId");

                    b.ToTable("NotificationMessage", (string)null);

                    b.HasDiscriminator<string>("Discriminator").HasValue("NotificationMessageEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationTemplateEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("LanguageCode")
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("ModifiedBy")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("NotificationId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("OuterId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.HasKey("Id");

                    b.HasIndex("NotificationId");

                    b.ToTable("NotificationTemplate", (string)null);

                    b.HasDiscriminator<string>("Discriminator").HasValue("NotificationTemplateEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity");

                    b.Property<string>("From")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("To")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.HasDiscriminator().HasValue("EmailNotificationEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationMessageEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationMessageEntity");

                    b.Property<string>("BCC")
                        .HasMaxLength(1024)
                        .HasColumnType("nvarchar(1024)");

                    b.Property<string>("Body")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CC")
                        .HasMaxLength(1024)
                        .HasColumnType("nvarchar(1024)");

                    b.Property<string>("From")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Subject")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.Property<string>("To")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.HasDiscriminator().HasValue("EmailNotificationMessageEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationTemplateEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationTemplateEntity");

                    b.Property<string>("Body")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NotificationLayoutId")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Sample")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Subject")
                        .HasMaxLength(512)
                        .HasColumnType("nvarchar(512)");

                    b.HasIndex("NotificationLayoutId");

                    b.HasDiscriminator().HasValue("EmailNotificationTemplateEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.SmsNotificationEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity");

                    b.Property<string>("Number")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.HasDiscriminator().HasValue("SmsNotificationEntity");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.SmsNotificationMessageEntity", b =>
                {
                    b.HasBaseType("VirtoCommerce.NotificationsModule.Data.Model.NotificationMessageEntity");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Number")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.HasDiscriminator().HasValue("SmsNotificationMessageEntity");
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

                    b.Navigation("Notification");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationEmailRecipientEntity", b =>
                {
                    b.HasOne("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationEntity", "Notification")
                        .WithMany("Recipients")
                        .HasForeignKey("NotificationId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Notification");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationMessageEntity", b =>
                {
                    b.HasOne("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity", "Notification")
                        .WithMany()
                        .HasForeignKey("NotificationId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Notification");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationTemplateEntity", b =>
                {
                    b.HasOne("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity", "Notification")
                        .WithMany("Templates")
                        .HasForeignKey("NotificationId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Notification");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationTemplateEntity", b =>
                {
                    b.HasOne("VirtoCommerce.NotificationsModule.Data.Model.NotificationLayoutEntity", "NotificationLayout")
                        .WithMany()
                        .HasForeignKey("NotificationLayoutId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("NotificationLayout");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.NotificationEntity", b =>
                {
                    b.Navigation("Templates");
                });

            modelBuilder.Entity("VirtoCommerce.NotificationsModule.Data.Model.EmailNotificationEntity", b =>
                {
                    b.Navigation("Attachments");

                    b.Navigation("Recipients");
                });
#pragma warning restore 612, 618
        }
    }
}
