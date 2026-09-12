import importlib.util
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
MODULE_PATH = ROOT / "Tools" / "pr_local_evidence_validator.py"
SPEC = importlib.util.spec_from_file_location("pr_local_evidence_validator", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class LocalFirstPREvidenceTests(unittest.TestCase):
    def test_complete_local_evidence_passes(self):
        body = """本地工作区：/mnt/data/555-workspace
本地提交：abc1234
本地测试：python -m unittest Tests/test_x.py -v -> PASS
"""
        self.assertEqual([], MODULE.validate_pr_body(body))

    def test_missing_field_fails(self):
        body = """本地工作区：/mnt/data/555-workspace
本地提交：abc1234
"""
        errors = MODULE.validate_pr_body(body)
        self.assertTrue(any("本地测试" in error for error in errors))

    def test_empty_field_fails(self):
        body = """本地工作区：/mnt/data/555-workspace
本地提交：
本地测试：pytest -> PASS
"""
        errors = MODULE.validate_pr_body(body)
        self.assertTrue(any("本地提交" in error for error in errors))

    def test_ascii_colon_is_accepted(self):
        body = """本地工作区: /tmp/repo
本地提交: deadbee
本地测试: python tests -> PASS
"""
        self.assertEqual([], MODULE.validate_pr_body(body))


if __name__ == "__main__":
    unittest.main()
