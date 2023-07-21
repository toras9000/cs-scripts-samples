#load ".common.csx"
#nullable enable
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Displays a list of books accessible to API users.

var settings = new
{
    // API base address for BookStack.(Trailing slash is required.)
    ApiEntry = new Uri(@"http://localhost:9986/api/"),
};

// main processing
await Paved.RunAsync(configuration: o => o.AnyPause(), action: async () =>
{
    // Set output to UTF8 encoding.
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    // Handle cancel key press
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    // Show caption
    ConsoleWig.WriteLine($"API entrypoint : {settings.ApiEntry}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(settings.ApiEntry, signal.Token);

    // Create an API client.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var helper = new BookStackClientHelper(client);

    // Image downloader
    using var downloader = new HttpClient();

    // Determine output directory
    var outdir = ThisSource.RelativeDirectory($"{ThisSource.File().BaseName()}-{DateTime.Now:yyyyMMdd-HHmmss}");

    // Create export context
    var expContext = new ExportContext(helper, downloader, signal.Token);

    // Retrieve all owned book information.
    var paging = 1;
    var processed = 0;
    while (true)
    {
        // Search for own books.
        var found = await helper.Try(c => c.SearchAsync(new("{type:book} {owned_by:me}", count: 100, page: paging), signal.Token));

        // If API access is successful, scramble and save the API key.
        if (paging == 1)
        {
            await info.SaveAsync();
        }

        // Retrieve information for each book
        foreach (var item in found.books())
        {
            ConsoleWig.WriteLine($"Exporting: {item.name} ...");

            // Reading book contents
            var book = await helper.Try(c => c.ReadBookAsync(item.id, signal.Token));
            var bookDir = outdir.RelativeDirectory($"B[{book.id}].{book.name.ToFileName()}").WithCreate();
            var bookContext = new BookExportContext(expContext, bookDir);

            // Output the contents.
            foreach (var (content, contentIdx) in book.contents.Select((c, i) => (c, i)))
            {
                if (content is BookContentChapter chapterContent)
                {
                    var chapter = await helper.Try(c => c.ReadChapterAsync(chapterContent.id, signal.Token));
                    foreach (var (pageContent, pipageIndex) in chapterContent.pages.CoalesceEmpty().Select((c, i) => (c, i)))
                    {
                        await exportPageAsync(bookContext, $"{contentIdx:D3}C.{pipageIndex:D3}P", pageContent);
                    }
                }
                else if (content is BookContentPage pageContent)
                {
                    await exportPageAsync(bookContext, $"{contentIdx:D3}P", pageContent);
                }
            }
        }

        // Update search information and determine end of search.
        paging++;
        processed += found.data.Length;
        if (found.data.Length <= 0 || found.total <= processed) break;
    }

});

// Export context data
record ExportContext(BookStackClientHelper Helper, HttpClient Downloader, CancellationToken CancelToken);
record BookExportContext(ExportContext Exporting, DirectoryInfo BookDir) : ExportContext(Exporting);

// Export page content
async Task exportPageAsync(BookExportContext context, string identify, BookContentPage pageContent)
{
    var page = await context.Helper.Try(c => c.ReadPageAsync(pageContent.id, context.CancelToken));
    if (page.markdown.IsNotWhite()) await context.BookDir.RelativeFile($"{identify}_{page.name.ToFileName()}.md").WriteAllTextAsync(page.markdown, context.CancelToken);
    else if (page.raw_html.IsNotWhite()) await context.BookDir.RelativeFile($"{identify}_{page.name.ToFileName()}.html").WriteAllTextAsync(page.html, context.CancelToken);

    // export page attachments (only file attachment)
    var attachCount = 0;
    var attachInPage = new Filter[] { new(nameof(AttachmentItem.uploaded_to), $"{page.id}") };
    var attachDir = context.BookDir.RelativeDirectory($"{identify}_attachments");
    while (true)
    {
        var attachments = await context.Helper.Try(c => c.ListAttachmentsAsync(new(attachCount, count: 100, filters: attachInPage), context.CancelToken));
        foreach (var item in attachments.data)
        {
            if (item.external) continue;
            var attach = await context.Helper.Try(c => c.ReadAttachmentAsync(item.id, context.CancelToken));
            var bin = Convert.FromBase64String(attach.content);
            var file = attachDir.RelativeFile($"A[{attach.id}].{attach.name.ToFileName()}".Mux(attach.extension, ".")).WithDirectoryCreate();
            await file.WriteAllBytesAsync(bin, context.CancelToken);
        }
        attachCount += attachments.data.Length;
        if (attachments.data.Length <= 0 || attachments.total <= attachCount) break;
    }

    // export image-gallery
    var imageCount = 0;
    var imageInPage = new Filter[] { new(nameof(ImageSummary.uploaded_to), $"{page.id}") };
    var imageDir = context.BookDir.RelativeDirectory($"{identify}_images");
    while (true)
    {
        var iamges = await context.Helper.Try(c => c.ListImagesAsync(new(imageCount, count: 100, filters: imageInPage), context.CancelToken));
        foreach (var image in iamges.data)
        {
            using var download = await context.Downloader.GetStreamAsync(image.url, context.CancelToken);
            var urlname = Path.GetFileName(image.url);
            var file = imageDir.RelativeFile($"I[{image.id}].{urlname.ToFileName()}").WithDirectoryCreate();
            using var fileStream = file.OpenWrite();
            await download.CopyToAsync(fileStream, context.CancelToken);
        }
        imageCount += iamges.data.Length;
        if (iamges.data.Length <= 0 || iamges.total <= imageCount) break;
    }
}