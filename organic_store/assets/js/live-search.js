$(document).ready(function () {
    $('.live-search').on('input', function () {
        let $this = $(this);
        let keyword = $this.val().trim();
        let url = $this.data('url');
        let target = $this.data('target');

        $.ajax({
            url: url,
            type: 'GET',
            data: { q: keyword },
            success: function (data) {
                $(target).html(data);
                if (keyword.length > 0) {
                    $('.pagination').hide();
                } else {
                    $('.pagination').show();
                }
            }
        });
    });
});
