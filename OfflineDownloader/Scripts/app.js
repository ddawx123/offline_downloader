// JavaScript Document

$(document).ready(function () {
    getfilelist();
});

function delobj(file) {
    $.ajax({
        url: "../Api/Delete",
        method: "DELETE",
        dataType: "json",
        data: {
            mFileName: file,
            rndKeywords: btoa(Math.random())
        },
        async: true,
        success: function (data) {
            if (data.state == "success") {
                showFailure("文件删除成功");
                getfilelist();
            } else {
                showFailure("文件删除失败，建议刷新页面或稍后重试！", 2000);
            }
        },
        error: function (e) {
            showFailure("文件删除失败，建议刷新页面或稍后重试！", 2000);
        }
    })
}

function getfilelist() {
    $.ajax({
        url: "../Api/getFileList",
        method: "GET",
        dataType: "xml",
        data: {
            rndKeywords: btoa(Math.random())
        },
        async: true,
        success: function (data) {
            $("#fileListBox").html("");
            for (var i = 0; i < $(data).find('mFileInfo').length; i++) {
                var fileName = $(data).find('mFileInfo')[i].getElementsByTagName('fileName')[0].innerHTML;
                var fileSize = $(data).find('mFileInfo')[i].getElementsByTagName('fileSize')[0].innerHTML;
                $("#fileListBox").append("<tr><td>" + (i + 1) + "</td><td>" + fileName + "</td><td>" + (fileSize / 1024 / 1024).toFixed(3) + "M</td><td><a href=\"./Content/UserFiles/" + fileName + "\">下载</a></td><td><button class=\"btn btn-danger btn-sm\" onclick=\"delobj('" + fileName + "')\" type=\"button\">删除</button></td></tr>");
            }
            $("#fileListBox").append("<tr></tr>");
            console.log();
        },
        error: function (e) {
            $("#fileListBox").html("<tr><td colspan=\"4\">抱歉，您需要登录后方可查询文件列表。</td></tr>");
        }
    });
}

function sendrequest() {
    var url = $("#destination").val();
    if (url == "") {
        showFailure("请输入有效的网址后再次发起下载请求，谢谢！", 2600);
    } else {
        $.ajax({
            url: "../Api/Download",
            method: "POST",
            dataType: "json",
            data: {
                destination: url,
                rndKeywords: btoa(Math.random())
            },
            async: true,
            beforeSend: function () {
                showSuccess("任务已加入远程下载队列，请稍等片刻！", 0);
            },
            success: function (data) {
                if (data.state == "success") {
                    getfilelist();
                    showSuccess("文件下载成功，列表已重新加载。");
                } else if (data.state == "error") {
                    showFailure("文件下载失败，错误详情：" + data.msg + "<br>温馨提示：平台仅支持创建时长小于等于30分钟的下载任务，如出现任务队列中某下载进程超时占用资源，系统将予以释放并删除相关文件！");
                } else {
                    showFailure("您可能没有权限启动下载，建议检查是否已经完成登录！");
                }
            },
            error: function (e) {
                showFailure("您可能没有权限启动下载，建议检查是否已经完成登录！");
            }
        });
    }
    return false;
}

function showSuccess(msg, timeout = 1500) {
    $("#notice").removeClass("bg-danger");
    $("#notice").addClass("bg-success");
    $("#notice").html(msg);
    $("#notice").show();
    if (timeout > 0) {
        setTimeout(function () {
            $("#notice").hide();
        }, timeout);
    }
}

function showFailure(msg, timeout = 1500) {
    $("#notice").removeClass("bg-success");
    $("#notice").addClass("bg-danger");
    $("#notice").html(msg);
    $("#notice").show();
    if (timeout > 0) {
        setTimeout(function () {
            $("#notice").hide();
        }, timeout);
    }
}