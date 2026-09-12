#!/usr/bin/env python3
import os
import re
import sys

REQUIRED_FIELDS = (
    "本地工作区",
    "本地提交",
    "本地测试",
)


def validate_pr_body(body: str):
    errors = []
    for field in REQUIRED_FIELDS:
        match = re.search(
            rf"(?m)^[ \t]*{re.escape(field)}[ \t]*[：:][ \t]*([^\r\n]+?)[ \t]*$",
            body or "",
        )
        if not match or not match.group(1).strip():
            errors.append(f"PR 缺少非空本地证据字段：{field}：")
    return errors


def main():
    body = os.environ.get("LOCAL_PR_BODY", "")
    errors = validate_pr_body(body)
    if errors:
        print("LOCAL-FIRST PR EVIDENCE VALIDATION FAILED")
        for error in errors:
            print("-", error)
        return 1
    print("LOCAL-FIRST PR EVIDENCE VALIDATION PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())
