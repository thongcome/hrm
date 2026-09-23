using HRM.Services.Km;
using Xunit;

namespace HRM.Tests.Km;

// KM article search: LIKE finds candidates, this pure scorer ranks them. Weights are internal
// tuning, not a business rule — tests check ORDER/relative ranking, not exact score numbers.
public class KmSearchRelevanceTests
{
    [Fact]
    public void No_search_term_scores_zero()
    {
        Assert.Equal(0, KmSearchRelevance.Score("ใดๆ", "แท็ก", "เนื้อหา", null, viewCount: 999));
        Assert.Equal(0, KmSearchRelevance.Score("ใดๆ", "แท็ก", "เนื้อหา", "  ", viewCount: 999));
    }

    [Fact]
    public void Title_match_beats_a_body_only_mention()
    {
        var titleHit = KmSearchRelevance.Score("การลาพักร้อน", null, "เนื้อหาทั่วไปไม่เกี่ยวข้อง", "ลาพักร้อน", viewCount: 0);
        var bodyOnlyHit = KmSearchRelevance.Score("เรื่องอื่นโดยสิ้นเชิง", null, "มีการพูดถึงลาพักร้อนแทรกอยู่ตรงนี้", "ลาพักร้อน", viewCount: 0);
        Assert.True(titleHit > bodyOnlyHit);
    }

    [Fact]
    public void Exact_title_match_beats_partial_title_match()
    {
        var exact = KmSearchRelevance.Score("ลาพักร้อน", null, "", "ลาพักร้อน", viewCount: 0);
        var partial = KmSearchRelevance.Score("นโยบายการลาพักร้อนของบริษัท", null, "", "ลาพักร้อน", viewCount: 0);
        Assert.True(exact > partial);
    }

    [Fact]
    public void All_search_words_found_in_title_outranks_only_some()
    {
        var both = KmSearchRelevance.Score("แผนพัฒนารายบุคคล IDP", null, "", "แผน IDP", viewCount: 0);
        var onlyOne = KmSearchRelevance.Score("แผนงบประมาณประจำปี", null, "IDP ไม่เกี่ยวกับเรื่องนี้เลย", "แผน IDP", viewCount: 0);
        Assert.True(both > onlyOne);
    }

    [Fact]
    public void Tag_match_outranks_content_only_match()
    {
        var tagHit = KmSearchRelevance.Score("บทความทั่วไป", "PDPA, ความเป็นส่วนตัว", "ไม่มีคำนี้ในเนื้อหา", "PDPA", viewCount: 0);
        var contentHit = KmSearchRelevance.Score("บทความทั่วไป", null, "มีคำว่า PDPA อยู่ในเนื้อหา", "PDPA", viewCount: 0);
        Assert.True(tagHit > contentHit);
    }

    [Fact]
    public void Popularity_only_breaks_ties_never_outweighs_a_real_text_match()
    {
        var weakMatchHighViews = KmSearchRelevance.Score("เรื่องไม่เกี่ยวข้อง", null, "แทรกคำว่า ลา ไว้นิดเดียว", "ลา", viewCount: 100_000);
        var strongMatchNoViews = KmSearchRelevance.Score("การลา", null, "", "ลา", viewCount: 0);
        Assert.True(strongMatchNoViews > weakMatchHighViews);
    }

    [Fact]
    public void Among_equal_text_relevance_more_views_ranks_higher()
    {
        var popular = KmSearchRelevance.Score("การลาพักร้อน", null, "", "ลาพักร้อน", viewCount: 500);
        var unseen = KmSearchRelevance.Score("การลาพักร้อน", null, "", "ลาพักร้อน", viewCount: 0);
        Assert.True(popular > unseen);
    }

    [Fact]
    public void No_match_anywhere_scores_zero()
    {
        Assert.Equal(0, KmSearchRelevance.Score("หัวข้ออื่น", "แท็กอื่น", "เนื้อหาอื่น", "คำที่ไม่มีอยู่เลย", viewCount: 0));
    }
}
