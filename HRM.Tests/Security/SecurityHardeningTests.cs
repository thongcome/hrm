using HRM.Data;
using HRM.Models;
using HRM.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Xunit;

namespace HRM.Tests.Security;

// ช่องโหว่ที่ ADP.AI แจ้ง 26 ก.ย. 2569 (โค้ดชุดเดียวกับ HRM) — เทสไว้กันกลับมาเป็นซ้ำ
public class SecurityHardeningTests
{
    // รหัสผ่านชั่วคราวต้องสุ่มใหม่ทุกครั้ง ไม่ใช่ค่าคงที่เดิม "Abcd@2025" ที่เปิดบัญชีใหม่ของทุกลูกค้าได้
    [Fact]
    public void Temporary_password_is_never_the_same_twice()
    {
        var generated = Enumerable.Range(0, 50).Select(_ => TemporaryPassword.New()).ToList();
        Assert.Equal(generated.Count, generated.Distinct().Count());
        Assert.DoesNotContain("Abcd@2025", generated);
    }

    // ต้องผ่านนโยบายของ Identity: ยาวพอ มีตัวใหญ่ ตัวเล็ก ตัวเลข และสัญลักษณ์
    [Fact]
    public void Temporary_password_satisfies_the_identity_policy()
    {
        foreach (var pw in Enumerable.Range(0, 20).Select(_ => TemporaryPassword.New()))
        {
            Assert.Equal(16, pw.Length);
            Assert.Contains(pw, char.IsUpper);
            Assert.Contains(pw, char.IsLower);
            Assert.Contains(pw, char.IsDigit);
            Assert.Contains(pw, c => !char.IsLetterOrDigit(c));
            // ตัวที่อ่าน/พิมพ์ต่อทางโทรศัพท์แล้วสับสน ต้องไม่มี
            Assert.DoesNotContain(pw, c => c is 'I' or 'l' or 'O' or '0' or '1');
        }
    }

    // บทบาทสองอันชื่อซ้ำกัน (ไม่มี unique index บน sc_role.name) ต้องไม่ทำให้ตัวตรวจสิทธิ์ระเบิด
    // เดิมใช้ ToDictionary ตรง ๆ → ArgumentException แล้วทุกหน้าที่เรียก GetRightsAsync พังทั้งระบบ
    [Fact]
    public void Duplicate_role_names_map_to_every_matching_role()
    {
        var map = ProgramRoleService.BuildRoleMap(
        [
            new("ฝ่ายบุคคล", 1),
            new("ฝ่ายบุคคล", 2),    // ชื่อซ้ำ คนละ roleid — เกิดได้จริง หน้าจัดการบทบาทไม่ได้ห้าม
            new("Admin", 9),
        ]);

        Assert.Equal([1L, 2L], map["ฝ่ายบุคคล"]);
        Assert.Equal([9L], map["admin"]);   // เทียบชื่อไม่สนตัวพิมพ์ เหมือน claim ที่ login ส่งมา
    }

    // สิทธิ์ของบทบาทชื่อซ้ำต้องรวมกันแบบ union (RBAC สิทธิ์เป็นบวกเสมอ) ไม่ใช่หยิบอันใดอันหนึ่ง
    [Fact]
    public void Rights_from_two_roles_with_the_same_name_are_combined()
    {
        IReadOnlyList<sc_program_role> readOnly = [new() { roleid = 1, progpath = "/pay", canread = true, isactive = true }];
        IReadOnlyList<sc_program_role> editor = [new() { roleid = 2, progpath = "/pay", canedit = true, isactive = true }];

        var rights = ProgramRoleService.ResolveRights([readOnly, editor], "/pay");

        Assert.True(rights.CanRead);
        Assert.True(rights.CanEdit);
    }
}